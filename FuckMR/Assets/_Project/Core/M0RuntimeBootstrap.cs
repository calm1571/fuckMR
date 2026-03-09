using System;
using System.Collections;
using Project.Gameplay.Combat;
using Project.Gameplay.Input;
using Project.MRWorld;
using Project.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.PXR;

namespace Project.Core
{
    public sealed class M0RuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private float menuDistance = 2.2f;
        [SerializeField] private float menuVerticalOffset = -0.02f;
        [SerializeField] private float calibrationMoveSpeed = 1.2f;
        [SerializeField] private float calibrationRotateSpeed = 55f;
        [SerializeField] private float calibrationHeightSpeed = 0.45f;
        [SerializeField] private int networkPort = 27777;
        [SerializeField] private string hostIpForClient = "192.168.50.2";
        [SerializeField] private float poseSendRate = 30f;
        [SerializeField] private int worldRootSyncBurstCount = 5;
        [SerializeField] private float worldRootSyncBurstInterval = 0.12f;

        private static M0RuntimeBootstrap _instance;

        private AppStateMachine _stateMachine;
        private IPlayerInputSource _inputSource;
        private M1InputDebugProbe _inputDebugProbe;
        private M1ProjectileShooter _projectileShooter;
        private M1AlwaysVisibleControllerLaser _alwaysVisibleLaser;
        private CalibrationView _calibrationView;
        private WorldRootController _worldRootController;
        private GameObject _worldRootMarker;
        private RoleSelectView _roleSelectView;
        private LobbyView _lobbyHostView;
        private LobbyView _lobbyClientView;
        private M3NetworkCoordinator _networkCoordinator;
        private M3RemotePlayerProxy _remoteProxy;
        private NetworkRole _selectedRole = NetworkRole.None;
        private Transform _worldRootTransform;
        private bool _clientWorldRootLocked;
        private Coroutine _worldRootSyncRoutine;
        private ActionBasedController _leftActionController;
        private ActionBasedController _rightActionController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            var existing = FindObjectOfType<M0RuntimeBootstrap>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject("_ProjectBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<M0RuntimeBootstrap>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            var waitFrames = 0;
            while (Camera.main == null && waitFrames < 180)
            {
                waitFrames++;
                yield return null;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("M0Bootstrap: Main Camera not found.");
                yield break;
            }

            EnsurePicoManager(camera);
            ConfigureMainCamera(camera);
            RegisterVstStatusLog();
            yield return RequestMrPermissionWithSdk();
            EnsureOfficialPassthrough(camera);
            StartCoroutine(EnableOfficialPassthroughWithRetry());

            var menuView = new MainMenuView(camera.transform, HandleStartClicked, OnExitClicked, menuDistance, menuVerticalOffset);
            _roleSelectView = new RoleSelectView(camera.transform, OnHostSelected, OnClientSelected, HandleRoleSelectBackClicked, menuDistance, menuVerticalOffset);
            _lobbyHostView = new LobbyView(camera.transform, "Lobby Host", "Start Match", HandleHostStartMatchClicked, HandleLobbyBackClicked, menuDistance, menuVerticalOffset);
            _lobbyClientView = new LobbyView(camera.transform, "Lobby Client", "Waiting Host", null, HandleLobbyBackClicked, menuDistance, menuVerticalOffset);
            _calibrationView = new CalibrationView(camera.transform, HandleCalibrationConfirmClicked, HandleCalibrationBackClicked, menuDistance, menuVerticalOffset);

            _worldRootTransform = EnsureWorldRootExists();
            _worldRootController = new WorldRootController(
                _worldRootTransform,
                camera.transform,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _worldRootMarker = EnsureWorldRootMarker(_worldRootTransform);
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(false);
            }

            _stateMachine = new AppStateMachine();
            _stateMachine.Register(new BootState(() => _stateMachine.ChangeState(AppStateId.MainMenu)));
            _stateMachine.Register(new MainMenuState(menuView));
            _stateMachine.Register(new RoleSelectState(_roleSelectView));
            _stateMachine.Register(new LobbyHostState(_lobbyHostView, OnTickLobbyHost));
            _stateMachine.Register(new LobbyClientState(_lobbyClientView, OnTickLobbyClient));
            _stateMachine.Register(new CalibrationState(OnEnterCalibration, OnExitCalibration, OnTickCalibration));
            _stateMachine.Register(new PlayingState(OnEnterPlaying, OnExitPlaying));
            _stateMachine.ChangeState(AppStateId.Boot);

            _rightActionController = FindRightActionController();
            _leftActionController = FindLeftActionController();

            _inputSource = new PicoControllerInputSource(_rightActionController, useRightController: true);
            _inputDebugProbe = new M1InputDebugProbe(_inputSource);
            _projectileShooter = gameObject.GetComponent<M1ProjectileShooter>();
            if (_projectileShooter == null)
            {
                _projectileShooter = gameObject.AddComponent<M1ProjectileShooter>();
            }

            if (!_projectileShooter.HasShootOriginAssigned && _rightActionController != null)
            {
                var rayInteractor = _rightActionController.GetComponentInChildren<XRRayInteractor>(true);
                _projectileShooter.SetShootOrigin(rayInteractor != null ? rayInteractor.transform : _rightActionController.transform);
            }

            _projectileShooter.Bind(_inputSource);
            _projectileShooter.ShotFired += OnLocalShotFired;
            _projectileShooter.SetShootingEnabled(false);
            _alwaysVisibleLaser = gameObject.GetComponent<M1AlwaysVisibleControllerLaser>();
            if (_alwaysVisibleLaser == null)
            {
                _alwaysVisibleLaser = gameObject.AddComponent<M1AlwaysVisibleControllerLaser>();
            }

            _alwaysVisibleLaser.enabled = false;
            RefreshRayVisuals();

            _networkCoordinator = new M3NetworkCoordinator(networkPort, hostIpForClient, poseSendRate);
            _networkCoordinator.BindLocalRig(
                camera.transform,
                _leftActionController != null ? _leftActionController.transform : null,
                _rightActionController != null ? _rightActionController.transform : null);
            _networkCoordinator.RemoteCalibrationRequested += OnRemoteCalibrationRequested;
            _networkCoordinator.WorldRootSyncReceived += OnRemoteWorldRootSyncReceived;
            _networkCoordinator.RemoteShootReceived += OnRemoteShootReceived;

            _remoteProxy = gameObject.GetComponent<M3RemotePlayerProxy>();
            if (_remoteProxy == null)
            {
                _remoteProxy = gameObject.AddComponent<M3RemotePlayerProxy>();
            }
        }

        private void Update()
        {
            _inputSource?.Tick();
            _inputDebugProbe?.Tick();
            _networkCoordinator?.Tick(Time.unscaledTime);
            if (_networkCoordinator != null && _networkCoordinator.HasRemotePose && _remoteProxy != null)
            {
                _remoteProxy.ApplyPose(_networkCoordinator.LatestRemotePose);
            }

            _stateMachine?.Tick();
        }

        private void OnDestroy()
        {
            if (_projectileShooter != null)
            {
                _projectileShooter.ShotFired -= OnLocalShotFired;
            }

            if (_networkCoordinator != null)
            {
                _networkCoordinator.RemoteCalibrationRequested -= OnRemoteCalibrationRequested;
                _networkCoordinator.WorldRootSyncReceived -= OnRemoteWorldRootSyncReceived;
                _networkCoordinator.RemoteShootReceived -= OnRemoteShootReceived;
                _networkCoordinator.Stop();
            }
        }

        private static IEnumerator RequestMrPermissionWithSdk()
        {
            var done = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            PXR_PermissionRequest.RequestUserPermissionMR(
                _ =>
                {
                    done = true;
                    Debug.LogWarning("PXR MR permission denied.");
                },
                _ =>
                {
                    done = true;
                    Debug.Log("PXR MR permission granted.");
                },
                _ =>
                {
                    done = true;
                    Debug.LogWarning("PXR MR permission denied and don't ask again.");
                });
#else
            done = true;
#endif

            var timeout = 0f;
            while (!done && timeout < 10f)
            {
                timeout += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator EnableOfficialPassthroughWithRetry()
        {
            const int maxRetry = 30;
            for (var i = 0; i < maxRetry; i++)
            {
                PXR_Manager.EnableVideoSeeThrough = true;
                PXR_MixedReality.EnableVideoSeeThroughEffect(true);
                PXR_Plugin.Boundary.UPxr_SetSeeThroughBackground(true);
                yield return new WaitForSeconds(0.25f);
            }
        }

        private static void EnsureOfficialPassthrough(Camera mainCamera)
        {
            if (mainCamera.GetComponent<PXR_CameraEffectBlock>() == null)
            {
                mainCamera.gameObject.AddComponent<PXR_CameraEffectBlock>();
            }
        }

        private static void RegisterVstStatusLog()
        {
            PXR_Manager.VstDisplayStatusChanged -= OnVstDisplayStatusChanged;
            PXR_Manager.VstDisplayStatusChanged += OnVstDisplayStatusChanged;
        }

        private static void OnVstDisplayStatusChanged(PxrVstStatus status)
        {
            Debug.Log($"PXR VST status: {status}");
        }

        private static void ConfigureMainCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

        private static void EnsurePicoManager(Camera mainCamera)
        {
            if (mainCamera == null)
            {
                return;
            }

            if (mainCamera.GetComponentInParent<PXR_Manager>() != null)
            {
                return;
            }

            var target = mainCamera.transform.root != null ? mainCamera.transform.root.gameObject : mainCamera.gameObject;
            target.AddComponent<PXR_Manager>();
        }

        private void HandleStartClicked()
        {
            _stateMachine?.ChangeState(AppStateId.RoleSelect);
        }

        private void HandleRoleSelectBackClicked()
        {
            _stateMachine?.ChangeState(AppStateId.MainMenu);
        }

        private void OnHostSelected()
        {
            _selectedRole = NetworkRole.Host;
            _networkCoordinator?.StartHost();
            _stateMachine?.ChangeState(AppStateId.LobbyHost);
        }

        private void OnClientSelected()
        {
            _selectedRole = NetworkRole.Client;
            _networkCoordinator?.StartClient(hostIpForClient);
            _stateMachine?.ChangeState(AppStateId.LobbyClient);
        }

        private void HandleLobbyBackClicked()
        {
            _networkCoordinator?.Stop();
            _selectedRole = NetworkRole.None;
            _clientWorldRootLocked = false;
            _stateMachine?.ChangeState(AppStateId.RoleSelect);
        }

        private void OnTickLobbyHost()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            _lobbyHostView.SetStatus(_networkCoordinator.BuildLobbyStatus());
            _lobbyHostView.SetPrimaryButton(_networkCoordinator.IsConnected ? "Start Match" : "Waiting Client...", _networkCoordinator.IsConnected);
        }

        private void OnTickLobbyClient()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            _lobbyClientView.SetStatus(_networkCoordinator.BuildLobbyStatus());
            _lobbyClientView.SetPrimaryButton("Waiting Host", false);
        }

        private void HandleHostStartMatchClicked()
        {
            if (_selectedRole != NetworkRole.Host || _networkCoordinator == null || !_networkCoordinator.IsConnected)
            {
                return;
            }

            _networkCoordinator.NotifyHostStartCalibration();
            _stateMachine?.ChangeState(AppStateId.Calibration);
        }

        private void OnRemoteCalibrationRequested()
        {
            if (_selectedRole == NetworkRole.Client)
            {
                _stateMachine?.ChangeState(AppStateId.Calibration);
            }
        }

        private void HandleCalibrationConfirmClicked()
        {
            if (_selectedRole == NetworkRole.Client && !_clientWorldRootLocked)
            {
                _calibrationView?.SetStatus("Waiting host confirmation...");
                return;
            }

            if (_selectedRole == NetworkRole.Host)
            {
                if (_networkCoordinator == null || !_networkCoordinator.IsConnected)
                {
                    _calibrationView?.SetStatus("Client disconnected. Please reconnect.");
                    return;
                }

                if (_worldRootTransform != null)
                {
                    if (_worldRootSyncRoutine != null)
                    {
                        StopCoroutine(_worldRootSyncRoutine);
                    }

                    _worldRootSyncRoutine = StartCoroutine(BroadcastWorldRootSyncBurst());
                }
            }

            _stateMachine?.ChangeState(AppStateId.Playing);
        }

        private void HandleCalibrationBackClicked()
        {
            if (_selectedRole == NetworkRole.Host)
            {
                _stateMachine?.ChangeState(AppStateId.LobbyHost);
                return;
            }

            if (_selectedRole == NetworkRole.Client)
            {
                _stateMachine?.ChangeState(AppStateId.LobbyClient);
                return;
            }

            _stateMachine?.ChangeState(AppStateId.MainMenu);
        }

        private void OnEnterCalibration()
        {
            _clientWorldRootLocked = false;
            _projectileShooter?.SetShootingEnabled(false);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = true;
            }

            _calibrationView?.SetVisible(true);
            if (_selectedRole == NetworkRole.Client)
            {
                _calibrationView?.SetStatus("Waiting host confirmation...\nYou can fine-tune locally before lock.");
            }
            else
            {
                _calibrationView?.SetStatus(_worldRootController != null ? _worldRootController.BuildStatusText() : "WorldRoot unavailable");
            }
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(true);
            }

            RefreshRayVisuals();
            Debug.Log("M2: Enter Calibration");
        }

        private void OnExitCalibration()
        {
            _calibrationView?.SetVisible(false);
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(false);
            }

            if (_worldRootSyncRoutine != null)
            {
                StopCoroutine(_worldRootSyncRoutine);
                _worldRootSyncRoutine = null;
            }
        }

        private void OnTickCalibration()
        {
            if (!_clientWorldRootLocked)
            {
                _worldRootController?.Tick(Time.deltaTime);
            }

            _calibrationView?.Tick();
            if (_worldRootController != null)
            {
                var baseStatus = _worldRootController.BuildStatusText();
                if (_selectedRole == NetworkRole.Host)
                {
                    _calibrationView?.SetStatus(baseStatus + "\nHost Confirm will broadcast WorldRoot.");
                }
                else if (_selectedRole == NetworkRole.Client)
                {
                    var suffix = _clientWorldRootLocked ? "\nWorldRoot locked by host. Entering match..." : "\nWaiting host confirmation...";
                    _calibrationView?.SetStatus(baseStatus + suffix);
                }
                else
                {
                    _calibrationView?.SetStatus(baseStatus);
                }
            }
        }

        private void OnRemoteWorldRootSyncReceived(WorldRootSyncPayload payload)
        {
            if (_selectedRole != NetworkRole.Client || payload == null)
            {
                return;
            }

            if (_worldRootTransform != null)
            {
                _worldRootTransform.SetPositionAndRotation(payload.position, payload.rotation);
                _clientWorldRootLocked = true;
                Debug.Log($"M4: Client applied WorldRoot sync: pos={payload.position}, rotY={payload.rotation.eulerAngles.y:F1}");
            }

            if (_stateMachine != null && _stateMachine.CurrentId == AppStateId.Calibration)
            {
                _stateMachine.ChangeState(AppStateId.Playing);
            }
        }

        private void OnLocalShotFired(M1ProjectileShooter.ShotInfo shot)
        {
            _networkCoordinator?.NotifyShoot(
                shot.spawnPosition,
                shot.direction,
                shot.speed,
                shot.maxDistance,
                shot.lifetime);
        }

        private void OnRemoteShootReceived(ShootPayload shot)
        {
            if (_projectileShooter == null || shot == null || _stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing)
            {
                return;
            }

            _projectileShooter.SpawnRemoteProjectile(
                shot.spawnPosition,
                shot.direction,
                shot.speed,
                shot.maxDistance,
                shot.lifetime);
        }

        private IEnumerator BroadcastWorldRootSyncBurst()
        {
            if (_networkCoordinator == null || _worldRootTransform == null)
            {
                yield break;
            }

            var count = Mathf.Max(1, worldRootSyncBurstCount);
            var interval = Mathf.Max(0.02f, worldRootSyncBurstInterval);
            for (var i = 0; i < count; i++)
            {
                _networkCoordinator.NotifyHostStartCalibration();
                _networkCoordinator.NotifyHostWorldRootSync(_worldRootTransform.position, _worldRootTransform.rotation);
                yield return new WaitForSecondsRealtime(interval);
            }

            _worldRootSyncRoutine = null;
        }

        private void OnEnterPlaying()
        {
            _projectileShooter?.SetShootingEnabled(true);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = true;
            }
            RefreshRayVisuals();
            Debug.Log("M1: Enter Playing");
        }

        private void OnExitPlaying()
        {
            _projectileShooter?.SetShootingEnabled(false);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = false;
            }

            _calibrationView?.SetVisible(false);
        }

        private static void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshRayVisuals()
        {
            var rays = FindObjectsOfType<XRRayInteractor>(true);
            var gradient = BuildCyanGradient();

            for (var i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                if (ray == null || !ray.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var lineVisual = ray.GetComponent<XRInteractorLineVisual>();
                if (lineVisual != null)
                {
                    lineVisual.enabled = true;
                    lineVisual.overrideInteractorLineLength = true;
                    lineVisual.lineLength = 8f;
                    lineVisual.autoAdjustLineLength = false;
                    lineVisual.stopLineAtFirstRaycastHit = false;
                    lineVisual.lineWidth = 0.006f;
                    lineVisual.validColorGradient = gradient;
                    lineVisual.invalidColorGradient = gradient;
                    lineVisual.blockedColorGradient = gradient;
                }

                var lineRenderer = ray.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.startWidth = 0.006f;
                    lineRenderer.endWidth = 0.004f;
                }
            }
        }

        private static ActionBasedController FindRightActionController()
        {
            var controllers = FindObjectsOfType<ActionBasedController>(true);
            for (var i = 0; i < controllers.Length; i++)
            {
                var candidate = controllers[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.gameObject.name.Contains("Right"))
                {
                    return candidate;
                }
            }

            return controllers.Length > 0 ? controllers[0] : null;
        }

        private static ActionBasedController FindLeftActionController()
        {
            var controllers = FindObjectsOfType<ActionBasedController>(true);
            for (var i = 0; i < controllers.Length; i++)
            {
                var candidate = controllers[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.gameObject.name.Contains("Left"))
                {
                    return candidate;
                }
            }

            return controllers.Length > 0 ? controllers[0] : null;
        }

        private static Transform EnsureWorldRootExists()
        {
            var existing = GameObject.Find("WorldRoot");
            if (existing != null)
            {
                return existing.transform;
            }

            var root = new GameObject("WorldRoot");
            return root.transform;
        }

        private static GameObject EnsureWorldRootMarker(Transform worldRoot)
        {
            if (worldRoot == null)
            {
                return null;
            }

            var existing = worldRoot.Find("WorldRootMarker");
            if (existing != null)
            {
                return existing.gameObject;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "WorldRootMarker";
            marker.transform.SetParent(worldRoot, false);
            marker.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            marker.transform.localScale = new Vector3(0.28f, 0.14f, 0.28f);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.95f, 0.55f, 0.1f, 1f);
                    renderer.material = mat;
                }
            }

            return marker;
        }

        private static Gradient BuildCyanGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.18f, 0.95f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.12f, 0.75f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }
    }
}
