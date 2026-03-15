using System;
using System.Collections;
using System.Threading.Tasks;
using Project.Gameplay.Combat;
using Project.Gameplay.Input;
using Project.MRWorld;
using Project.Networking;
using Project.ScriptableObjects;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.PXR;

namespace Project.Core
{
    public sealed class M0RuntimeBootstrap : MonoBehaviour
    {
        private const string BuildStamp = "MR-RESULT-RETRY-V5";

        private enum LiveCalibrationPhase
        {
            ClientAdjustHost = 0,
            HostAdjustClient = 1,
            ReadyToPlay = 2
        }

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
        [SerializeField] private float calibrationSyncInterval = 0.08f;
        [SerializeField] private CombatBalanceConfig combatBalanceConfig;
        [SerializeField] private float hitCheckRadius = 0.25f;
        [SerializeField] private bool enableSharedSpatialAnchor = true;
        [SerializeField] private bool enableAutoAprilTagTracking = true;
        [SerializeField] private bool disableAprilTagCalibrationTemporarily = true;
        [SerializeField] private bool enableManualMarkerLock = true;
        [SerializeField] private string aprilTagFamily = "36h11";
        [SerializeField] private int aprilTagId = 0;
        [SerializeField] private float aprilTagSizeMm = 95f;
        [SerializeField] private int aprilTagFrameWidth = 640;
        [SerializeField] private int aprilTagFrameHeight = 480;
        [SerializeField] private float aprilTagVisualSizeMeters = 0.1f;
        [SerializeField] private float aprilTagVisualLineThicknessMeters = 0.006f;
        [SerializeField] private float aprilTagVisualLiftMeters = 0.002f;
        [SerializeField] private float calibrationReadyStabilityThreshold = 0.65f;
        [SerializeField] private float calibrationReadyHoldSeconds = 0.4f;
        [SerializeField] private float calibrationReadySendInterval = 0.25f;
        [SerializeField] private float hudDistance = 1.15f;
        [SerializeField] private Vector3 hudLocalOffset = new Vector3(0f, 0.28f, 0f);

        private static M0RuntimeBootstrap _instance;

        private AppStateMachine _stateMachine;
        private IPlayerInputSource _inputSource;
        private M1InputDebugProbe _inputDebugProbe;
        private M1ProjectileShooter _projectileShooter;
        private M1AlwaysVisibleControllerLaser _alwaysVisibleLaser;
        private CalibrationView _calibrationView;
        private WorldRootController _worldRootController;
        private RemoteAlignmentController _remoteAlignmentController;
        private GameObject _worldRootMarker;
        private Transform _remoteAlignmentRoot;
        private RoleSelectView _roleSelectView;
        private LobbyView _lobbyHostView;
        private LobbyView _lobbyClientView;
        private LobbyView _resultView;
        private M5PlayerHudView _playerHudView;
        private M3NetworkCoordinator _networkCoordinator;
        private M3RemotePlayerProxy _remoteProxy;
        private NetworkRole _selectedRole = NetworkRole.None;
        private Transform _worldRootTransform;
        private bool _clientWorldRootLocked;
        private Coroutine _worldRootSyncRoutine;
        private Coroutine _sharedAnchorPublishRoutine;
        private Coroutine _sharedAnchorResolveRoutine;
        private float _nextCalibrationSyncTime;
        private float _nextRemoteAlignmentSyncTime;
        private ActionBasedController _leftActionController;
        private ActionBasedController _rightActionController;
        private M5ShieldVisual _localShieldVisual;
        private M5ShieldVisual _remoteShieldVisual;

        private int _hostHp;
        private int _clientHp;
        private float _hostShieldEndTime;
        private float _clientShieldEndTime;
        private float _hostShieldCooldownUntil;
        private float _clientShieldCooldownUntil;
        private float _hostNextShootAllowedTime;
        private float _clientNextShootAllowedTime;
        private float _localShootCooldownUntil;
        private string _resultText = "Result";
        private SpatialAnchorSyncService _spatialAnchorService;
        private IMarkerTrackingSource _markerTrackingSource;
        private MarkerTrackingSample _markerSample;
        private bool _hasMarkerSample;
        private GameObject _aprilTagTrackingFrame;
        private bool _localCalibrationReady;
        private bool _remoteCalibrationReady;
        private float _localCalibrationReadySince = -1f;
        private float _lastCalibrationReadySendTime;
        private bool _lastSentCalibrationReady;
        private LiveCalibrationPhase _liveCalibrationPhase;
        private bool _clientAlignmentConfirmed;
        private bool _hostAlignmentConfirmed;
        private bool _localRematchReady;
        private bool _remoteRematchReady;

        private bool IsAprilTagCalibrationActive => enableAutoAprilTagTracking && !disableAprilTagCalibrationTemporarily;

        private bool CanAdjustLiveRemoteAlignment()
        {
            if (IsAprilTagCalibrationActive)
            {
                return false;
            }

            return (_selectedRole == NetworkRole.Client && _liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost) ||
                   (_selectedRole == NetworkRole.Host && _liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient);
        }

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
            _resultView = new LobbyView(camera.transform, "Result", "Retry", HandleResultRetryClicked, HandleResultBackToMenuClicked, menuDistance, menuVerticalOffset);
            _playerHudView = new M5PlayerHudView(camera.transform, hudDistance, hudLocalOffset);

            _worldRootTransform = EnsureWorldRootExists();
            _worldRootController = new WorldRootController(
                _worldRootTransform,
                camera.transform,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _remoteAlignmentRoot = EnsureRemoteAlignmentRootExists();
            _remoteAlignmentController = new RemoteAlignmentController(
                _remoteAlignmentRoot,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _worldRootMarker = EnsureWorldRootMarker(_worldRootTransform);
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(false);
            }

            _aprilTagTrackingFrame = EnsureAprilTagTrackingFrame(aprilTagVisualSizeMeters, aprilTagVisualLineThicknessMeters);
            if (_aprilTagTrackingFrame != null)
            {
                _aprilTagTrackingFrame.SetActive(false);
            }

            _stateMachine = new AppStateMachine();
            _stateMachine.Register(new BootState(() => _stateMachine.ChangeState(AppStateId.MainMenu)));
            _stateMachine.Register(new MainMenuState(menuView));
            _stateMachine.Register(new RoleSelectState(_roleSelectView));
            _stateMachine.Register(new LobbyHostState(_lobbyHostView, OnTickLobbyHost));
            _stateMachine.Register(new LobbyClientState(_lobbyClientView, OnTickLobbyClient));
            _stateMachine.Register(new CalibrationState(OnEnterCalibration, OnExitCalibration, OnTickCalibration));
            _stateMachine.Register(new PlayingState(OnEnterPlaying, OnExitPlaying));
            _stateMachine.Register(new ResultState(OnEnterResult, OnExitResult, OnTickResult));
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
                // Keep projectile origin aligned with the visible right-hand proxy.
                _projectileShooter.SetShootOrigin(_rightActionController.transform);
            }

            _projectileShooter.Bind(_inputSource);
            _projectileShooter.ShotFired += OnLocalShotFired;
            _projectileShooter.SetShootingEnabled(false);
            _projectileShooter.SetCombatTuning(GetProjectileSpeed(), GetProjectileRadius(), GetShootCooldown());
            _inputSource.AButtonDown += OnLocalShieldPressed;
            _alwaysVisibleLaser = gameObject.GetComponent<M1AlwaysVisibleControllerLaser>();
            if (_alwaysVisibleLaser == null)
            {
                _alwaysVisibleLaser = gameObject.AddComponent<M1AlwaysVisibleControllerLaser>();
            }

            _alwaysVisibleLaser.enabled = false;
            RefreshRayVisuals();
            _hostHp = GetMaxHp();
            _clientHp = GetMaxHp();

            _networkCoordinator = new M3NetworkCoordinator(networkPort, hostIpForClient, poseSendRate);
            _networkCoordinator.BindLocalRig(
                camera.transform,
                _leftActionController != null ? _leftActionController.transform : null,
                _rightActionController != null ? _rightActionController.transform : null);
            _networkCoordinator.RemoteCalibrationRequested += OnRemoteCalibrationRequested;
            _networkCoordinator.WorldRootSyncReceived += OnRemoteWorldRootSyncReceived;
            _networkCoordinator.RemoteShootReceived += OnRemoteShootReceived;
            _networkCoordinator.RemoteShieldReceived += OnRemoteShieldReceived;
            _networkCoordinator.HpUpdateReceived += OnHpUpdateReceived;
            _networkCoordinator.MatchResultReceived += OnMatchResultReceived;
            _networkCoordinator.SharedAnchorReceived += OnSharedAnchorReceived;
            _networkCoordinator.StartPlayingRequested += OnStartPlayingRequested;
            _networkCoordinator.RemoteCalibrationReadyReceived += OnRemoteCalibrationReadyReceived;
            _networkCoordinator.RemoteAlignmentReceived += OnRemoteAlignmentReceived;
            _networkCoordinator.RemoteRematchReadyReceived += OnRemoteRematchReadyReceived;

            _remoteProxy = gameObject.GetComponent<M3RemotePlayerProxy>();
            if (_remoteProxy == null)
            {
                _remoteProxy = gameObject.AddComponent<M3RemotePlayerProxy>();
            }
            _remoteProxy.BindAlignmentRoot(_remoteAlignmentRoot);
            _remoteAlignmentController?.SetPivotTransform(_remoteProxy.HeadTransform);

            _spatialAnchorService = new SpatialAnchorSyncService();
            var manualMarkerSource = new ManualMarkerTrackingSource();
            if (IsAprilTagCalibrationActive)
            {
                var config = new AprilTagTrackingConfig(
                    aprilTagFamily,
                    aprilTagId,
                    aprilTagSizeMm * 0.001f,
                    aprilTagFrameWidth,
                    aprilTagFrameHeight);
                IAprilTagDetector detector = new OpenCVForUnityAprilTagDetector();
                var autoMarkerSource = new AprilTagAutoTrackingSource(config, detector);
                _markerTrackingSource = new AutoOrManualMarkerTrackingSource(autoMarkerSource, manualMarkerSource);
            }
            else
            {
                _markerTrackingSource = manualMarkerSource;
            }
            UpdateEnemyHealthBar();
            _playerHudView?.SetStatus(GetMaxHp(), GetMaxHp(), 0f, 0f);
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
            TickCombat();
            _playerHudView?.Tick();
        }

        private void OnDestroy()
        {
            if (_sharedAnchorPublishRoutine != null)
            {
                StopCoroutine(_sharedAnchorPublishRoutine);
                _sharedAnchorPublishRoutine = null;
            }

            if (_sharedAnchorResolveRoutine != null)
            {
                StopCoroutine(_sharedAnchorResolveRoutine);
                _sharedAnchorResolveRoutine = null;
            }

            if (_projectileShooter != null)
            {
                _projectileShooter.ShotFired -= OnLocalShotFired;
            }

            if (_inputSource != null)
            {
                _inputSource.AButtonDown -= OnLocalShieldPressed;
            }

            if (_networkCoordinator != null)
            {
                _networkCoordinator.RemoteCalibrationRequested -= OnRemoteCalibrationRequested;
                _networkCoordinator.WorldRootSyncReceived -= OnRemoteWorldRootSyncReceived;
                _networkCoordinator.RemoteShootReceived -= OnRemoteShootReceived;
                _networkCoordinator.RemoteShieldReceived -= OnRemoteShieldReceived;
                _networkCoordinator.HpUpdateReceived -= OnHpUpdateReceived;
                _networkCoordinator.MatchResultReceived -= OnMatchResultReceived;
                _networkCoordinator.SharedAnchorReceived -= OnSharedAnchorReceived;
                _networkCoordinator.StartPlayingRequested -= OnStartPlayingRequested;
                _networkCoordinator.RemoteCalibrationReadyReceived -= OnRemoteCalibrationReadyReceived;
                _networkCoordinator.RemoteAlignmentReceived -= OnRemoteAlignmentReceived;
                _networkCoordinator.RemoteRematchReadyReceived -= OnRemoteRematchReadyReceived;
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

            var existingManagers = FindObjectsOfType<PXR_Manager>(true);
            if (existingManagers != null && existingManagers.Length > 0)
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
            if (enableSharedSpatialAnchor)
            {
                StartCoroutine(WarmupSpatialAnchorProviderRoutine());
            }
            _stateMachine?.ChangeState(AppStateId.LobbyHost);
        }

        private void OnClientSelected()
        {
            _selectedRole = NetworkRole.Client;
            _networkCoordinator?.StartClient(hostIpForClient);
            if (enableSharedSpatialAnchor)
            {
                StartCoroutine(WarmupSpatialAnchorProviderRoutine());
            }
            _stateMachine?.ChangeState(AppStateId.LobbyClient);
        }

        private void HandleLobbyBackClicked()
        {
            _networkCoordinator?.Stop();
            _selectedRole = NetworkRole.None;
            _clientWorldRootLocked = false;
            if (_sharedAnchorPublishRoutine != null)
            {
                StopCoroutine(_sharedAnchorPublishRoutine);
                _sharedAnchorPublishRoutine = null;
            }

            if (_sharedAnchorResolveRoutine != null)
            {
                StopCoroutine(_sharedAnchorResolveRoutine);
                _sharedAnchorResolveRoutine = null;
            }
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

            ResetCombatForNewMatch();
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
                if (!IsAprilTagCalibrationActive)
                {
                    if (_liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost)
                    {
                        if (_networkCoordinator == null || !_networkCoordinator.IsConnected || !_networkCoordinator.HasRemotePose)
                        {
                            _calibrationView?.SetStatus("Waiting remote avatar stream before client adjustment.");
                            return;
                        }

                        _clientAlignmentConfirmed = true;
                        _liveCalibrationPhase = LiveCalibrationPhase.HostAdjustClient;
                        _networkCoordinator?.NotifyRemoteAlignment(
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.position : Vector3.zero,
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.rotation : Quaternion.identity,
                            true,
                            LiveCalibrationPhase.ClientAdjustHost.ToString());
                        _calibrationView?.SetStatus("Client step confirmed.\nWaiting for host to adjust your avatar.");
                        return;
                    }

                    _calibrationView?.SetStatus(_liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient
                        ? "Waiting for host to finish adjustment."
                        : "Waiting for host final Confirm to start match.");
                    return;
                }

                var local = _localCalibrationReady ? "READY" : "WAIT";
                var remote = _remoteCalibrationReady ? "READY" : "WAIT";
                _calibrationView?.SetStatus($"Waiting host confirmation...\nDual-ready gate L/R: {local}/{remote}");
                return;
            }

            if (_selectedRole == NetworkRole.Host)
            {
                if (!IsAprilTagCalibrationActive)
                {
                    if (_liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost)
                    {
                        _calibrationView?.SetStatus(_clientAlignmentConfirmed
                            ? "Client confirmed host alignment.\nHost may proceed to phase 2."
                            : "Waiting for client to align host avatar and confirm.");
                        return;
                    }

                    if (_liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient)
                    {
                        if (_networkCoordinator == null || !_networkCoordinator.IsConnected || !_networkCoordinator.HasRemotePose)
                        {
                            _calibrationView?.SetStatus("Waiting remote avatar stream before host adjustment.");
                            return;
                        }

                        _hostAlignmentConfirmed = true;
                        _liveCalibrationPhase = LiveCalibrationPhase.ReadyToPlay;
                        _networkCoordinator?.NotifyRemoteAlignment(
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.position : Vector3.zero,
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.rotation : Quaternion.identity,
                            true,
                            LiveCalibrationPhase.HostAdjustClient.ToString());
                        _calibrationView?.SetStatus("Host step confirmed.\nPress Confirm again to start match.");
                        return;
                    }

                    if (!_clientAlignmentConfirmed || !_hostAlignmentConfirmed)
                    {
                        _calibrationView?.SetStatus("Calibration steps incomplete.\nClient and Host must both confirm their own view.");
                        return;
                    }
                }

                if (IsAprilTagCalibrationActive)
                {
                    if (!_hasMarkerSample || !_markerSample.hasPose || _markerSample.sourceMode != MarkerTrackingSourceMode.AutoAprilTag)
                    {
                        _calibrationView?.SetStatus("AprilTag not located yet.\nKeep tag 36h11 / ID0 fully visible until auto ready.");
                        return;
                    }

                    ApplyWorldRootFromMarkerPose(_markerSample.pose);
                }
                else if (enableManualMarkerLock && !disableAprilTagCalibrationTemporarily)
                {
                    if (!_hasMarkerSample || !_markerSample.hasPose || !_markerSample.isLocked)
                    {
                        _calibrationView?.SetStatus("Lock marker first (X). If wrong, unlock with Y.");
                        return;
                    }

                    ApplyWorldRootFromMarkerPose(_markerSample.pose);
                }

                if (IsAprilTagCalibrationActive && (!_localCalibrationReady || !_remoteCalibrationReady))
                {
                    _calibrationView?.SetStatus("Dual-ready gate not met.\nBoth Host and Client must be READY before Confirm.");
                    return;
                }

                if (_networkCoordinator == null || !_networkCoordinator.IsConnected)
                {
                    _calibrationView?.SetStatus("Client disconnected. Please reconnect.");
                    return;
                }

                if (IsAprilTagCalibrationActive && _worldRootTransform != null)
                {
                    if (_worldRootSyncRoutine != null)
                    {
                        StopCoroutine(_worldRootSyncRoutine);
                    }

                    _worldRootSyncRoutine = StartCoroutine(BroadcastWorldRootSyncBurst());
                    _networkCoordinator.NotifyHostWorldRootSync(_worldRootTransform.position, _worldRootTransform.rotation);
                }

                if (IsAprilTagCalibrationActive && enableSharedSpatialAnchor)
                {
                    if (_sharedAnchorPublishRoutine != null)
                    {
                        StopCoroutine(_sharedAnchorPublishRoutine);
                    }

                    _sharedAnchorPublishRoutine = StartCoroutine(PublishSharedAnchorRoutine());
                }
            }

            if (_selectedRole == NetworkRole.Host)
            {
                _networkCoordinator?.NotifyHostStartPlaying();
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
            _nextCalibrationSyncTime = 0f;
            _nextRemoteAlignmentSyncTime = 0f;
            _hasMarkerSample = false;
            _localCalibrationReady = false;
            _remoteCalibrationReady = false;
            _liveCalibrationPhase = IsAprilTagCalibrationActive ? LiveCalibrationPhase.ReadyToPlay : LiveCalibrationPhase.ClientAdjustHost;
            _clientAlignmentConfirmed = false;
            _hostAlignmentConfirmed = false;
            _localCalibrationReadySince = -1f;
            _lastCalibrationReadySendTime = -999f;
            _lastSentCalibrationReady = false;
            if (_remoteAlignmentRoot != null)
            {
                _remoteAlignmentRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            _projectileShooter?.SetShootingEnabled(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _markerTrackingSource?.Begin();
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = true;
            }

            _calibrationView?.SetVisible(true);
            _calibrationView?.SetConfirmVisible(true);
            _calibrationView?.SetConfirmText(IsAprilTagCalibrationActive ? "Confirm" : "Confirm Step");
            if (_selectedRole == NetworkRole.Client)
            {
                _calibrationView?.SetStatus(IsAprilTagCalibrationActive
                    ? "Waiting host confirmation...\nAuto AprilTag localization in progress."
                    : "Phase 1/3: Adjust Host avatar on Client.\nFine-tune, then press Confirm Step.");
                _calibrationView?.SetDetectionStatus(IsAprilTagCalibrationActive
                    ? "<color=#6CA9D9>Detection: searching AprilTag automatically...</color>"
                    : "<color=#6CA9D9>Detection: AprilTag disabled. Live remote alignment only.</color>");
            }
            else
            {
                _calibrationView?.SetStatus(IsAprilTagCalibrationActive
                    ? (_worldRootController != null ? _worldRootController.BuildStatusText() : "WorldRoot unavailable")
                    : "Phase 1/3: Waiting for Client to adjust Host avatar.");
                _calibrationView?.SetDetectionStatus(IsAprilTagCalibrationActive
                    ? "<color=#6CA9D9>Detection: searching AprilTag automatically...</color>"
                    : "<color=#6CA9D9>Detection: AprilTag disabled. Use live refine and Confirm when ready.</color>");
            }
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(true);
            }

            if (_aprilTagTrackingFrame != null)
            {
                _aprilTagTrackingFrame.SetActive(false);
            }

            RefreshRayVisuals();
            Debug.Log("M2: Enter Calibration");
        }

        private void OnExitCalibration()
        {
            if (_networkCoordinator != null && _networkCoordinator.IsConnected && _selectedRole != NetworkRole.None)
            {
                _networkCoordinator.NotifyCalibrationReady(false, false, false, 0f);
            }

            _calibrationView?.SetVisible(false);
            _calibrationView?.SetConfirmVisible(true);
            _markerTrackingSource?.End();
            if (_worldRootMarker != null)
            {
                _worldRootMarker.SetActive(false);
            }

            if (_aprilTagTrackingFrame != null)
            {
                _aprilTagTrackingFrame.SetActive(false);
            }

            if (_worldRootSyncRoutine != null)
            {
                StopCoroutine(_worldRootSyncRoutine);
                _worldRootSyncRoutine = null;
            }
        }

        private void OnTickCalibration()
        {
            var now = Time.unscaledTime;
            if (!_clientWorldRootLocked && !IsAprilTagCalibrationActive && !disableAprilTagCalibrationTemporarily)
            {
                _worldRootController?.Tick(Time.deltaTime);
            }

            if (CanAdjustLiveRemoteAlignment() && _networkCoordinator != null && _networkCoordinator.HasRemotePose)
            {
                _remoteAlignmentController?.Tick(Time.deltaTime);
            }

            if (IsAprilTagCalibrationActive && _markerTrackingSource != null)
            {
                _markerTrackingSource.Tick(Time.deltaTime);
                _hasMarkerSample = _markerTrackingSource.TryGetSample(out _markerSample);
            }
            else
            {
                _hasMarkerSample = false;
            }

            UpdateLocalCalibrationReady(now);
            SendCalibrationReadyIfNeeded(now);
            UpdateAprilTagTrackingFrameVisual();

            if (IsAprilTagCalibrationActive &&
                _selectedRole == NetworkRole.Host &&
                _hasMarkerSample &&
                _markerSample.hasPose &&
                _markerSample.sourceMode == MarkerTrackingSourceMode.AutoAprilTag)
            {
                ApplyWorldRootFromMarkerPose(_markerSample.pose);
            }
            else if (_selectedRole == NetworkRole.Host && enableManualMarkerLock && !disableAprilTagCalibrationTemporarily && _hasMarkerSample && _markerSample.hasPose && _markerSample.isLocked)
            {
                ApplyWorldRootFromMarkerPose(_markerSample.pose);
            }

            _calibrationView?.Tick();
            _calibrationView?.SetDetectionStatus(BuildCalibrationDetectionText());
            if (IsAprilTagCalibrationActive)
            {
                if (_selectedRole == NetworkRole.Host)
                {
                    var readyText = (_localCalibrationReady && _remoteCalibrationReady)
                        ? "Both devices READY. Press Confirm to start."
                        : "Waiting for both devices to become READY.";
                    _calibrationView?.SetStatus($"Auto localization running.\nKeep AprilTag fully visible.\n{readyText}\n{BuildRemoteAlignmentStatusText()}");
                }
                else if (_selectedRole == NetworkRole.Client)
                {
                    var suffix = _clientWorldRootLocked
                        ? "WorldRoot locked by host. Entering match..."
                        : "Adjust remote avatar, then wait for host Confirm.";
                    _calibrationView?.SetStatus($"Auto localization running.\nKeep AprilTag fully visible.\n{suffix}\n{BuildRemoteAlignmentStatusText()}");
                }
                else
                {
                    _calibrationView?.SetStatus("Auto localization running.\nKeep AprilTag fully visible.");
                }
            }
            else
            {
                if (_selectedRole == NetworkRole.Host)
                {
                    _calibrationView?.SetConfirmText(_liveCalibrationPhase == LiveCalibrationPhase.ReadyToPlay ? "Confirm" : "Confirm Step");
                    string phaseText;
                    if (_liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost)
                    {
                        phaseText = _clientAlignmentConfirmed
                            ? "Phase 1/3 complete.\nPrepare to adjust Client avatar."
                            : "Phase 1/3: Waiting for Client to align Host avatar and confirm.";
                    }
                    else if (_liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient)
                    {
                        phaseText = "Phase 2/3: Host adjusts Client avatar.\nPress Confirm Step when done.";
                    }
                    else
                    {
                        phaseText = "Phase 3/3: Both steps locked.\nPress Confirm to start match.";
                    }

                    _calibrationView?.SetStatus($"Live remote alignment mode.\n{phaseText}\n{BuildRemoteAlignmentStatusText()}");
                }
                else if (_selectedRole == NetworkRole.Client)
                {
                    _calibrationView?.SetConfirmText(_liveCalibrationPhase == LiveCalibrationPhase.ReadyToPlay ? "Confirm" : "Confirm Step");
                    string phaseText;
                    if (_liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost)
                    {
                        phaseText = "Phase 1/3: Client adjusts Host avatar.\nPress Confirm Step when done.";
                    }
                    else if (_liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient)
                    {
                        phaseText = _hostAlignmentConfirmed
                            ? "Phase 2/3 complete.\nWaiting for host final Confirm."
                            : "Phase 2/3: Host is adjusting your avatar.\nPlease wait.";
                    }
                    else
                    {
                        phaseText = "Phase 3/3: Waiting for host final Confirm.";
                    }

                    _calibrationView?.SetStatus($"Live remote alignment mode.\n{phaseText}\n{BuildRemoteAlignmentStatusText()}");
                }
                else
                {
                    _calibrationView?.SetStatus("Live remote alignment mode.");
                }
            }

            if (IsAprilTagCalibrationActive &&
                _selectedRole == NetworkRole.Host &&
                _networkCoordinator != null &&
                _networkCoordinator.IsConnected &&
                _worldRootTransform != null &&
                Time.unscaledTime >= _nextCalibrationSyncTime)
            {
                _networkCoordinator.NotifyHostWorldRootSync(_worldRootTransform.position, _worldRootTransform.rotation);
                _nextCalibrationSyncTime = Time.unscaledTime + Mathf.Max(0.02f, calibrationSyncInterval);
            }

        }

        private string BuildCalibrationDetectionText()
        {
            if (_markerTrackingSource == null)
            {
                return "<color=#9DB2C6>Detection: tracking source unavailable</color>";
            }

            var gateText = $"Gate L/R: {(_localCalibrationReady ? "<color=#7CFF9A>READY</color>" : "<color=#FF8A8A>WAIT</color>")}/{(_remoteCalibrationReady ? "<color=#7CFF9A>READY</color>" : "<color=#FF8A8A>WAIT</color>")}";
            if (!IsAprilTagCalibrationActive)
            {
                return $"<color=#6CA9D9>Detection: AprilTag disabled for now. Use live remote alignment.</color>\n{gateText}";
            }

            var sourceText = _markerSample.sourceMode == MarkerTrackingSourceMode.AutoAprilTag ? "Source: Auto AprilTag" : "Source: Manual";
            if (_hasMarkerSample && _markerSample.hasPose)
            {
                var stability = Mathf.RoundToInt(Mathf.Clamp01(_markerSample.stability01) * 100f);
                if (IsAprilTagCalibrationActive && _markerSample.sourceMode == MarkerTrackingSourceMode.AutoAprilTag)
                {
                    if (_localCalibrationReady)
                    {
                        return $"<color=#7CFF9A>Detection: AprilTag auto-located ({stability}%).</color>\n{sourceText}\n{gateText}";
                    }

                    return $"<color=#FFD06D>Detection: AprilTag detected ({stability}%). Holding for auto-ready...</color>\n{sourceText}\n{gateText}";
                }

                if (_markerSample.isLocked)
                {
                    if (_selectedRole == NetworkRole.Host)
                    {
                        return $"<color=#7CFF9A>Detection: AprilTag locked ({stability}%).</color>\n{sourceText}\n{gateText}";
                    }

                    return $"<color=#7CFF9A>Detection: local AprilTag locked ({stability}%).</color>\n{sourceText}\n{gateText}";
                }

                if (_selectedRole == NetworkRole.Host)
                {
                    return $"<color=#FFD06D>Detection: AprilTag detected ({stability}%). Press X to lock marker.</color>\n{sourceText}\n{gateText}";
                }

                return $"<color=#FFD06D>Detection: local marker found ({stability}%). Press X to lock locally.</color>\n{sourceText}\n{gateText}";
            }

            return $"<color=#FF8A8A>Detection: AprilTag not found. Face tag 36h11 / ID0 (95mm) and keep it fully visible.</color>\n{gateText}";
        }

        private string BuildRemoteAlignmentStatusText()
        {
            if (_networkCoordinator == null || !_networkCoordinator.HasRemotePose || _remoteAlignmentController == null)
            {
                return "Remote refine: waiting remote avatar";
            }

            return _remoteAlignmentController.BuildStatusText();
        }

        private void UpdateLocalCalibrationReady(float now)
        {
            if (!IsAprilTagCalibrationActive)
            {
                var hasPose = _networkCoordinator != null && _networkCoordinator.IsConnected && _networkCoordinator.HasRemotePose;
                if (_liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost)
                {
                    _localCalibrationReady = _selectedRole == NetworkRole.Client ? hasPose : _clientAlignmentConfirmed;
                }
                else if (_liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient)
                {
                    _localCalibrationReady = _selectedRole == NetworkRole.Host ? hasPose : _hostAlignmentConfirmed;
                }
                else
                {
                    _localCalibrationReady = _clientAlignmentConfirmed && _hostAlignmentConfirmed;
                }

                _localCalibrationReadySince = _localCalibrationReady ? now : -1f;
                return;
            }

            if (_markerTrackingSource == null)
            {
                _localCalibrationReady = false;
                _localCalibrationReadySince = -1f;
                return;
            }

            bool stableEnough;
            if (IsAprilTagCalibrationActive)
            {
                stableEnough = _hasMarkerSample &&
                               _markerSample.hasPose &&
                               _markerSample.sourceMode == MarkerTrackingSourceMode.AutoAprilTag &&
                               _markerSample.stability01 >= Mathf.Clamp01(calibrationReadyStabilityThreshold);
            }
            else
            {
                stableEnough = _hasMarkerSample &&
                               _markerSample.hasPose &&
                               _markerSample.isLocked &&
                               _markerSample.stability01 >= Mathf.Clamp01(calibrationReadyStabilityThreshold);
            }

            if (stableEnough)
            {
                if (_localCalibrationReadySince < 0f)
                {
                    _localCalibrationReadySince = now;
                }
            }
            else
            {
                _localCalibrationReadySince = -1f;
            }

            var hold = Mathf.Max(0f, calibrationReadyHoldSeconds);
            _localCalibrationReady = stableEnough && _localCalibrationReadySince >= 0f && (now - _localCalibrationReadySince) >= hold;
        }

        private void SendCalibrationReadyIfNeeded(float now)
        {
            if (_networkCoordinator == null || !_networkCoordinator.IsConnected || _selectedRole == NetworkRole.None)
            {
                return;
            }

            var interval = Mathf.Max(0.05f, calibrationReadySendInterval);
            if (_lastSentCalibrationReady == _localCalibrationReady && now - _lastCalibrationReadySendTime < interval)
            {
                return;
            }

            var stability = _hasMarkerSample ? _markerSample.stability01 : 0f;
            var isLocked = IsAprilTagCalibrationActive ? _localCalibrationReady : _localCalibrationReady;
            _networkCoordinator.NotifyCalibrationReady(_localCalibrationReady, _hasMarkerSample, isLocked, stability);
            _lastCalibrationReadySendTime = now;
            _lastSentCalibrationReady = _localCalibrationReady;
        }

        private void UpdateAprilTagTrackingFrameVisual()
        {
            if (_aprilTagTrackingFrame == null)
            {
                return;
            }

            var shouldShow = _stateMachine != null &&
                             _stateMachine.CurrentId == AppStateId.Calibration &&
                             IsAprilTagCalibrationActive &&
                             _hasMarkerSample &&
                             _markerSample.hasPose &&
                             _markerSample.sourceMode == MarkerTrackingSourceMode.AutoAprilTag;
            _aprilTagTrackingFrame.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            var pose = _markerSample.pose;
            _aprilTagTrackingFrame.transform.SetPositionAndRotation(
                pose.position + pose.rotation * Vector3.forward * Mathf.Max(0f, aprilTagVisualLiftMeters),
                pose.rotation);
        }

        private void ApplyWorldRootFromMarkerPose(Pose markerPose)
        {
            if (_worldRootTransform == null)
            {
                return;
            }

            var yawOnly = Quaternion.Euler(0f, markerPose.rotation.eulerAngles.y, 0f);
            _worldRootTransform.SetPositionAndRotation(markerPose.position, yawOnly);
        }

        private void OnRemoteWorldRootSyncReceived(WorldRootSyncPayload payload)
        {
            if (_selectedRole != NetworkRole.Client || payload == null || !IsAprilTagCalibrationActive)
            {
                return;
            }

            if (_worldRootTransform != null)
            {
                _worldRootTransform.SetPositionAndRotation(payload.position, payload.rotation);
                _clientWorldRootLocked = true;
                Debug.Log($"M4: Client applied WorldRoot sync: pos={payload.position}, rotY={payload.rotation.eulerAngles.y:F1}");
            }
        }

        private void OnStartPlayingRequested()
        {
            if (_selectedRole != NetworkRole.Client || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.CurrentId == AppStateId.Result)
            {
                ResetCombatForNewMatch();
                _stateMachine.ChangeState(AppStateId.Playing);
                return;
            }

            if (_stateMachine.CurrentId == AppStateId.Calibration || _stateMachine.CurrentId == AppStateId.LobbyClient)
            {
                _stateMachine.ChangeState(AppStateId.Playing);
            }
        }

        private void OnRemoteCalibrationReadyReceived(CalibrationReadyPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            _remoteCalibrationReady = payload.ready;
        }

        private void OnRemoteAlignmentReceived(RemoteAlignmentPayload payload)
        {
            if (payload == null || IsAprilTagCalibrationActive)
            {
                return;
            }

            if (payload.senderRole == NetworkRole.Client.ToString() &&
                payload.stage == LiveCalibrationPhase.ClientAdjustHost.ToString() &&
                payload.confirmed)
            {
                _clientAlignmentConfirmed = true;
                _liveCalibrationPhase = LiveCalibrationPhase.HostAdjustClient;
                return;
            }

            if (payload.senderRole == NetworkRole.Host.ToString() &&
                payload.stage == LiveCalibrationPhase.HostAdjustClient.ToString() &&
                payload.confirmed)
            {
                _hostAlignmentConfirmed = true;
                _liveCalibrationPhase = LiveCalibrationPhase.ReadyToPlay;
            }
        }

        private void OnSharedAnchorReceived(SharedAnchorPayload payload)
        {
            if (!enableSharedSpatialAnchor || _selectedRole != NetworkRole.Client || payload == null || string.IsNullOrEmpty(payload.uuid))
            {
                return;
            }

            if (_sharedAnchorResolveRoutine != null)
            {
                StopCoroutine(_sharedAnchorResolveRoutine);
            }

            _sharedAnchorResolveRoutine = StartCoroutine(ResolveSharedAnchorRoutine(payload.uuid));
        }

        private void OnLocalShotFired(M1ProjectileShooter.ShotInfo shot)
        {
            if (_stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing || _networkCoordinator == null)
            {
                return;
            }

            _localShootCooldownUntil = Time.time + GetShootCooldown();
            if (_selectedRole == NetworkRole.Host)
            {
                if (Time.time < _hostNextShootAllowedTime)
                {
                    return;
                }

                _hostNextShootAllowedTime = Time.time + GetShootCooldown();
                HostResolveShotAgainstClient(shot);
            }

            _networkCoordinator.NotifyShoot(
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

            var visualSpawn = TransformRemotePositionForDisplay(shot.spawnPosition);
            var visualDirection = TransformRemoteDirectionForDisplay(shot.direction);
            _projectileShooter.SpawnRemoteProjectile(
                visualSpawn,
                visualDirection,
                shot.speed,
                shot.maxDistance,
                shot.lifetime);

            if (_selectedRole == NetworkRole.Host)
            {
                if (Time.time < _clientNextShootAllowedTime)
                {
                    return;
                }

                _clientNextShootAllowedTime = Time.time + GetShootCooldown();
                HostResolveShotAgainstHost(new M1ProjectileShooter.ShotInfo
                {
                    spawnPosition = visualSpawn,
                    direction = visualDirection,
                    speed = shot.speed,
                    maxDistance = shot.maxDistance,
                    lifetime = shot.lifetime
                });
            }
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

        private IEnumerator WarmupSpatialAnchorProviderRoutine()
        {
            if (_spatialAnchorService == null)
            {
                yield break;
            }

            var task = _spatialAnchorService.EnsureProviderReadyAsync();
            yield return WaitTask(task);
            if (!task.IsFaulted && !task.IsCanceled && task.Result)
            {
                Debug.Log("SpatialAnchor: Provider ready");
            }
            else
            {
                Debug.LogWarning("SpatialAnchor: Provider warmup failed, fallback to worldRoot sync");
            }
        }

        private IEnumerator PublishSharedAnchorRoutine()
        {
            if (_spatialAnchorService == null || _networkCoordinator == null || _worldRootTransform == null)
            {
                yield break;
            }

            var task = _spatialAnchorService.CreateOrReuseSharedAnchorAsync(_worldRootTransform.position, _worldRootTransform.rotation);
            yield return WaitTask(task);
            _sharedAnchorPublishRoutine = null;

            if (task.IsFaulted || task.IsCanceled || !task.Result.ok)
            {
                Debug.LogWarning("SpatialAnchor: Host publish failed, using manual worldRoot sync only");
                yield break;
            }

            _networkCoordinator.NotifyHostSharedAnchor(task.Result.uuid);
            Debug.Log($"SpatialAnchor: Host shared uuid={task.Result.uuid}");
        }

        private IEnumerator ResolveSharedAnchorRoutine(string uuid)
        {
            if (_spatialAnchorService == null || _worldRootTransform == null)
            {
                yield break;
            }

            _calibrationView?.SetStatus("Downloading shared anchor...");
            var task = _spatialAnchorService.DownloadAndLocateSharedAnchorAsync(uuid);
            yield return WaitTask(task);
            _sharedAnchorResolveRoutine = null;

            if (task.IsFaulted || task.IsCanceled || !task.Result.ok)
            {
                Debug.LogWarning("SpatialAnchor: Client resolve failed, fallback to manual worldRoot sync");
                yield break;
            }

            _worldRootTransform.SetPositionAndRotation(task.Result.position, task.Result.rotation);
            _clientWorldRootLocked = true;
            _calibrationView?.SetStatus("Shared anchor applied.");
            Debug.Log($"SpatialAnchor: Client applied uuid={uuid}");
        }

        private static IEnumerator WaitTask(Task task)
        {
            if (task == null)
            {
                yield break;
            }

            while (!task.IsCompleted)
            {
                yield return null;
            }
        }

        private void OnEnterPlaying()
        {
            if (_hostHp <= 0 || _clientHp <= 0)
            {
                _hostHp = GetMaxHp();
                _clientHp = GetMaxHp();
            }

            _projectileShooter?.SetShootingEnabled(true);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = true;
            }

            EnsureShieldVisuals();
            BindShieldAnchors();
            _playerHudView?.SetVisible(true);
            RefreshRayVisuals();
            Debug.Log($"M5: Enter Playing as {_selectedRole}. HostHP={_hostHp} ClientHP={_clientHp}");
        }

        private void OnExitPlaying()
        {
            _projectileShooter?.SetShootingEnabled(false);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = false;
            }

            _calibrationView?.SetVisible(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _playerHudView?.SetVisible(false);
        }

        private void OnEnterResult()
        {
            _projectileShooter?.SetShootingEnabled(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _localRematchReady = false;
            _remoteRematchReady = false;
            _resultView?.SetVisible(true);
            _resultView?.SetStatus(_resultText + $"\n[{BuildStamp}]\n\nPress Retry. Both players must confirm to rematch.");
            _resultView?.SetPrimaryButton("Retry", true);
            _playerHudView?.SetVisible(true);
            Debug.Log($"M5: Enter Result => {_resultText}");
        }

        private void OnExitResult()
        {
            _resultView?.SetVisible(false);
        }

        private void OnTickResult()
        {
            _resultView?.Tick();
        }

        private void HandleResultRetryClicked()
        {
            if (_networkCoordinator == null || !_networkCoordinator.IsConnected || _selectedRole == NetworkRole.None)
            {
                return;
            }

            _localRematchReady = true;
            _resultView?.SetPrimaryButton("Waiting Other...", false);
            _networkCoordinator.NotifyRematchReady(true);

            if (_selectedRole == NetworkRole.Host)
            {
                TryStartRematchAsHost();
            }
        }

        private void HandleResultBackToMenuClicked()
        {
            _networkCoordinator?.Stop();
            _selectedRole = NetworkRole.None;
            _clientWorldRootLocked = false;
            _stateMachine?.ChangeState(AppStateId.MainMenu);
        }

        private void OnLocalShieldPressed()
        {
            if (_stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing || _selectedRole == NetworkRole.None)
            {
                return;
            }

            if (_selectedRole == NetworkRole.Host)
            {
                if (TryActivateHostShield())
                {
                    _networkCoordinator?.NotifyShield(true, GetShieldDuration());
                }
            }
            else if (_selectedRole == NetworkRole.Client)
            {
                if (TryActivateClientShield())
                {
                    _networkCoordinator?.NotifyShield(true, GetShieldDuration());
                }
            }
        }

        private void OnRemoteShieldReceived(ShieldPayload payload)
        {
            if (payload == null || !payload.active)
            {
                return;
            }

            if (_selectedRole == NetworkRole.Host)
            {
                ActivateClientShieldAuthoritative(payload.duration);
            }
            else if (_selectedRole == NetworkRole.Client)
            {
                ActivateHostShieldVisual(payload.duration);
            }
        }

        private void OnHpUpdateReceived(HpUpdatePayload payload)
        {
            if (payload == null)
            {
                return;
            }

            _hostHp = payload.hostHp;
            _clientHp = payload.clientHp;
            Debug.Log($"M5: HP update => Host={_hostHp} Client={_clientHp}");
        }

        private void OnMatchResultReceived(MatchResultPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.winnerRole))
            {
                return;
            }

            var winner = payload.winnerRole;
            var localWon =
                (_selectedRole == NetworkRole.Host && winner == "Host") ||
                (_selectedRole == NetworkRole.Client && winner == "Client");
            _resultText = localWon ? "WIN" : "LOSE";
            if (_stateMachine != null && _stateMachine.CurrentId != AppStateId.Result)
            {
                _stateMachine.ChangeState(AppStateId.Result);
            }
        }

        private void OnRemoteRematchReadyReceived(RematchReadyPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            _remoteRematchReady = payload.ready;
            if (_stateMachine != null && _stateMachine.CurrentId == AppStateId.Result)
            {
                if (!_localRematchReady)
                {
                    _resultView?.SetStatus(_resultText + $"\n[{BuildStamp}]\n\nOther player is ready for retry.");
                }

                if (_selectedRole == NetworkRole.Host)
                {
                    TryStartRematchAsHost();
                }
            }
        }

        private void TryStartRematchAsHost()
        {
            if (_selectedRole != NetworkRole.Host || !_localRematchReady || !_remoteRematchReady)
            {
                return;
            }

            ResetCombatForNewMatch();
            _networkCoordinator?.NotifyHostStartPlaying();
            _stateMachine?.ChangeState(AppStateId.Playing);
        }

        private void TickCombat()
        {
            if (_stateMachine == null)
            {
                return;
            }

            BindShieldAnchors();
            UpdateEnemyHealthBar();
            var now = Time.time;
            if (_stateMachine.CurrentId == AppStateId.Playing)
            {
                if (_selectedRole == NetworkRole.Host)
                {
                    if (now >= _hostShieldEndTime)
                    {
                        _localShieldVisual?.Deactivate();
                    }

                    if (now >= _clientShieldEndTime)
                    {
                        _remoteShieldVisual?.Deactivate();
                    }
                }
                else if (_selectedRole == NetworkRole.Client)
                {
                    if (now >= _clientShieldEndTime)
                    {
                        _localShieldVisual?.Deactivate();
                    }

                    if (now >= _hostShieldEndTime)
                    {
                        _remoteShieldVisual?.Deactivate();
                    }
                }
            }

            UpdateLocalHud();
        }

        private void ResetCombatForNewMatch()
        {
            _hostHp = GetMaxHp();
            _clientHp = GetMaxHp();
            _hostShieldEndTime = -1f;
            _clientShieldEndTime = -1f;
            _hostShieldCooldownUntil = 0f;
            _clientShieldCooldownUntil = 0f;
            _hostNextShootAllowedTime = 0f;
            _clientNextShootAllowedTime = 0f;
            _localShootCooldownUntil = 0f;
            _resultText = "Result";
            _networkCoordinator?.NotifyHostHpUpdate(_hostHp, _clientHp);
            UpdateEnemyHealthBar();
        }

        private void HostResolveShotAgainstClient(M1ProjectileShooter.ShotInfo shot)
        {
            if (_clientHp <= 0)
            {
                return;
            }

            if (Time.time < _clientShieldEndTime)
            {
                Debug.Log("M5: Host shot blocked by Client shield");
                return;
            }

            if (!TryGetAlignedRemoteHeadPosition(out var remoteHeadPos) || !IsShotHitPosition(shot, remoteHeadPos))
            {
                return;
            }

            _clientHp = Mathf.Max(0, _clientHp - GetDamage());
            _networkCoordinator?.NotifyHostHpUpdate(_hostHp, _clientHp);
            Debug.Log($"M5: Host hit Client. Host={_hostHp} Client={_clientHp}");
            if (_clientHp <= 0)
            {
                EnterResultAsHost("Host");
            }
        }

        private void HostResolveShotAgainstHost(M1ProjectileShooter.ShotInfo shot)
        {
            if (_hostHp <= 0)
            {
                return;
            }

            if (Time.time < _hostShieldEndTime)
            {
                Debug.Log("M5: Client shot blocked by Host shield");
                return;
            }

            if (Camera.main == null || !IsShotHitPosition(shot, Camera.main.transform.position))
            {
                return;
            }

            _hostHp = Mathf.Max(0, _hostHp - GetDamage());
            _networkCoordinator?.NotifyHostHpUpdate(_hostHp, _clientHp);
            Debug.Log($"M5: Client hit Host. Host={_hostHp} Client={_clientHp}");
            if (_hostHp <= 0)
            {
                EnterResultAsHost("Client");
            }
        }

        private void EnterResultAsHost(string winnerRole)
        {
            _resultText = winnerRole == "Host" ? "WIN" : "LOSE";
            _networkCoordinator?.NotifyHostMatchResult(winnerRole);
            _stateMachine?.ChangeState(AppStateId.Result);
        }

        private bool TryGetAlignedRemoteHeadPosition(out Vector3 remoteHeadPosition)
        {
            remoteHeadPosition = Vector3.zero;
            if (_remoteProxy != null && _remoteProxy.HeadTransform != null)
            {
                remoteHeadPosition = _remoteProxy.HeadTransform.position;
                return true;
            }

            if (_networkCoordinator == null || !_networkCoordinator.HasRemotePose || _networkCoordinator.LatestRemotePose == null)
            {
                return false;
            }

            remoteHeadPosition = TransformRemotePositionForDisplay(_networkCoordinator.LatestRemotePose.head.position);
            return true;
        }

        private Vector3 TransformRemotePositionForDisplay(Vector3 rawPosition)
        {
            return _remoteAlignmentRoot != null ? _remoteAlignmentRoot.TransformPoint(rawPosition) : rawPosition;
        }

        private Vector3 TransformRemoteDirectionForDisplay(Vector3 rawDirection)
        {
            var dir = rawDirection.sqrMagnitude < 0.0001f ? Vector3.forward : rawDirection.normalized;
            return _remoteAlignmentRoot != null ? (_remoteAlignmentRoot.rotation * dir).normalized : dir;
        }

        private bool IsShotHitPosition(M1ProjectileShooter.ShotInfo shot, Vector3 targetPosition)
        {
            var dir = shot.direction.sqrMagnitude < 0.0001f ? Vector3.forward : shot.direction.normalized;
            var toTarget = targetPosition - shot.spawnPosition;
            var projection = Vector3.Dot(dir, toTarget);
            if (projection < 0f || projection > Mathf.Max(0.1f, shot.maxDistance))
            {
                return false;
            }

            var closest = shot.spawnPosition + dir * projection;
            var distance = Vector3.Distance(closest, targetPosition);
            return distance <= Mathf.Max(0.05f, hitCheckRadius + GetProjectileRadius());
        }

        private bool TryActivateHostShield()
        {
            var now = Time.time;
            if (now < _hostShieldCooldownUntil || now < _hostShieldEndTime)
            {
                return false;
            }

            var duration = GetShieldDuration();
            _hostShieldEndTime = now + duration;
            _hostShieldCooldownUntil = now + GetShieldCooldown();
            _localShieldVisual?.Activate(duration);
            return true;
        }

        private bool TryActivateClientShield()
        {
            var now = Time.time;
            if (now < _clientShieldCooldownUntil || now < _clientShieldEndTime)
            {
                return false;
            }

            var duration = GetShieldDuration();
            _clientShieldEndTime = now + duration;
            _clientShieldCooldownUntil = now + GetShieldCooldown();
            _localShieldVisual?.Activate(duration);
            return true;
        }

        private void ActivateClientShieldAuthoritative(float duration)
        {
            var now = Time.time;
            var d = Mathf.Max(0.1f, duration);
            _clientShieldEndTime = now + d;
            _clientShieldCooldownUntil = now + GetShieldCooldown();
            _remoteShieldVisual?.Activate(d);
        }

        private void ActivateHostShieldVisual(float duration)
        {
            var d = Mathf.Max(0.1f, duration);
            _hostShieldEndTime = Time.time + d;
            _remoteShieldVisual?.Activate(d);
        }

        private void EnsureShieldVisuals()
        {
            if (_localShieldVisual == null)
            {
                _localShieldVisual = gameObject.GetComponent<M5ShieldVisual>();
                if (_localShieldVisual == null)
                {
                    _localShieldVisual = gameObject.AddComponent<M5ShieldVisual>();
                }
            }

            if (_remoteShieldVisual == null)
            {
                var remoteShieldAnchor = transform.Find("RemoteShieldRoot");
                if (remoteShieldAnchor == null)
                {
                    var rootGo = new GameObject("RemoteShieldRoot");
                    rootGo.transform.SetParent(transform, false);
                    remoteShieldAnchor = rootGo.transform;
                }

                _remoteShieldVisual = remoteShieldAnchor.GetComponent<M5ShieldVisual>();
                if (_remoteShieldVisual == null)
                {
                    _remoteShieldVisual = remoteShieldAnchor.gameObject.AddComponent<M5ShieldVisual>();
                }
            }
        }

        private void BindShieldAnchors()
        {
            if (_localShieldVisual != null && Camera.main != null)
            {
                _localShieldVisual.BindAnchor(Camera.main.transform);
            }

            if (_remoteShieldVisual != null && _remoteProxy != null && _remoteProxy.HeadTransform != null)
            {
                _remoteShieldVisual.BindAnchor(_remoteProxy.HeadTransform);
            }
        }

        private int GetMaxHp() => combatBalanceConfig != null ? combatBalanceConfig.hp : 100;
        private int GetDamage() => combatBalanceConfig != null ? combatBalanceConfig.damage : 10;
        private float GetProjectileSpeed() => combatBalanceConfig != null ? combatBalanceConfig.projectileSpeed : 5f;
        private float GetProjectileRadius() => combatBalanceConfig != null ? combatBalanceConfig.projectileRadius : 0.033f;
        private float GetShootCooldown() => combatBalanceConfig != null ? combatBalanceConfig.shootCooldown : 1f;
        private float GetShieldDuration() => combatBalanceConfig != null ? combatBalanceConfig.shieldDuration : 1.5f;
        private float GetShieldCooldown() => combatBalanceConfig != null ? combatBalanceConfig.shieldCooldown : 3f;

        private void UpdateEnemyHealthBar()
        {
            if (_remoteProxy == null || _networkCoordinator == null || !_networkCoordinator.HasRemotePose)
            {
                return;
            }

            var maxHp = Mathf.Max(1, GetMaxHp());
            int enemyHp;
            if (_selectedRole == NetworkRole.Host)
            {
                enemyHp = _clientHp;
            }
            else if (_selectedRole == NetworkRole.Client)
            {
                enemyHp = _hostHp;
            }
            else
            {
                enemyHp = maxHp;
            }

            var normalized = Mathf.Clamp01(enemyHp / (float)maxHp);
            _remoteProxy.SetEnemyHealthNormalized(normalized);
        }

        private void UpdateLocalHud()
        {
            if (_playerHudView == null)
            {
                return;
            }

            var now = Time.time;
            int myHp;
            float myShieldCd;
            if (_selectedRole == NetworkRole.Host)
            {
                myHp = _hostHp;
                myShieldCd = Mathf.Max(0f, _hostShieldCooldownUntil - now);
            }
            else if (_selectedRole == NetworkRole.Client)
            {
                myHp = _clientHp;
                myShieldCd = Mathf.Max(0f, _clientShieldCooldownUntil - now);
            }
            else
            {
                myHp = GetMaxHp();
                myShieldCd = 0f;
            }

            var myShootCd = Mathf.Max(0f, _localShootCooldownUntil - now);
            _playerHudView.SetStatus(myHp, GetMaxHp(), myShootCd, myShieldCd);
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

        private static Transform EnsureRemoteAlignmentRootExists()
        {
            var existing = GameObject.Find("RemoteAlignmentRoot");
            if (existing != null)
            {
                return existing.transform;
            }

            var root = new GameObject("RemoteAlignmentRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root.transform;
        }

        private static GameObject EnsureAprilTagTrackingFrame(float sizeMeters, float lineThicknessMeters)
        {
            var existing = GameObject.Find("AprilTagTrackingFrame");
            if (existing != null)
            {
                return existing;
            }

            var frame = new GameObject("AprilTagTrackingFrame");
            frame.transform.position = Vector3.zero;
            frame.transform.rotation = Quaternion.identity;

            var size = Mathf.Clamp(sizeMeters, 0.04f, 0.4f);
            var thickness = Mathf.Clamp(lineThicknessMeters, 0.0015f, 0.03f);
            var half = size * 0.5f;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader);
                mat.color = new Color(0.15f, 1f, 0.95f, 1f);
            }

            CreateFrameEdge("Top", frame.transform, new Vector3(0f, half, 0f), new Vector3(size, thickness, thickness), mat);
            CreateFrameEdge("Bottom", frame.transform, new Vector3(0f, -half, 0f), new Vector3(size, thickness, thickness), mat);
            CreateFrameEdge("Left", frame.transform, new Vector3(-half, 0f, 0f), new Vector3(thickness, size, thickness), mat);
            CreateFrameEdge("Right", frame.transform, new Vector3(half, 0f, 0f), new Vector3(thickness, size, thickness), mat);
            CreateFrameEdge("Center", frame.transform, new Vector3(0f, 0f, 0f), new Vector3(thickness * 1.5f, thickness * 1.5f, thickness), mat);

            return frame;
        }

        private static void CreateFrameEdge(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = name;
            edge.transform.SetParent(parent, false);
            edge.transform.localPosition = localPos;
            edge.transform.localRotation = Quaternion.identity;
            edge.transform.localScale = localScale;
            ApplyMarkerPartStyle(edge, mat);
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
                // Upgrade old cube marker to the new arrow marker.
                if (existing.Find("ArrowBody") != null && existing.Find("ArrowTip") != null)
                {
                    return existing.gameObject;
                }

                Destroy(existing.gameObject);
            }

            var marker = new GameObject("WorldRootMarker");
            marker.transform.SetParent(worldRoot, false);
            marker.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader);
                mat.color = new Color(0.95f, 0.55f, 0.1f, 1f);
            }

            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlate.name = "BasePlate";
            basePlate.transform.SetParent(marker.transform, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            basePlate.transform.localScale = new Vector3(0.22f, 0.06f, 0.22f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "ArrowBody";
            body.transform.SetParent(marker.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.09f, 0.10f);
            body.transform.localScale = new Vector3(0.06f, 0.045f, 0.22f);

            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "ArrowTip";
            tip.transform.SetParent(marker.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.09f, 0.24f);
            tip.transform.localScale = new Vector3(0.12f, 0.045f, 0.08f);

            ApplyMarkerPartStyle(basePlate, mat);
            ApplyMarkerPartStyle(body, mat);
            ApplyMarkerPartStyle(tip, mat);

            return marker;
        }

        private static void ApplyMarkerPartStyle(GameObject part, Material mat)
        {
            if (part == null)
            {
                return;
            }

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null && mat != null)
            {
                renderer.material = mat;
            }
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
