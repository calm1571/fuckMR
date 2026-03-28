// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Runs the main runtime orchestration for gameplay, networking, calibration, and UI.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using System.Collections;
using System.Collections.Generic;
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
        /// <summary>
    /// 工程运行时总控，负责状态机、网络、MR 对齐、玩法与 Spectator 功能调度。
    /// </summary>
    public sealed class M0RuntimeBootstrap : MonoBehaviour
    {
        private const string BuildStamp = "MR-SPECTATOR-WALL-V1";
        private const string ObstacleArenaAnchorType = "ArenaCenter";
        private const string HostIpPlayerPrefsKey = "Project.Network.HostIp";

                /// <summary>
        /// 多角色串行校准阶段。
        /// </summary>
        private enum LiveCalibrationPhase
        {
            ClientAdjustHost = 0,
            HostAdjustClient = 1,
            SpectatorAdjustClient = 2,
            SpectatorAdjustHost = 3,
            HostFinalConfirm = 4
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
        [SerializeField] private SpectatorSupportConfig spectatorSupportConfig;
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
        [SerializeField] private float spectatorPanelDistance = 1.1f;
        [SerializeField] private Vector3 spectatorPanelLocalOffset = new Vector3(0f, -0.16f, 0f);
        [SerializeField] private float spectatorBarrageDistance = 1.2f;
        [SerializeField] private Vector3 spectatorBarrageLocalOffset = new Vector3(0f, 0f, 0f);

        private static M0RuntimeBootstrap _instance;

        private AppStateMachine _stateMachine;
        private IPlayerInputSource _inputSource;
        private M1InputDebugProbe _inputDebugProbe;
        private M1ProjectileShooter _projectileShooter;
        private M1AlwaysVisibleControllerLaser _alwaysVisibleLaser;
        private CalibrationView _calibrationView;
        private WorldRootController _worldRootController;
        private RemoteAlignmentController _remoteAlignmentController;
        private RemoteAlignmentController _spectatorHostAlignmentController;
        private RemoteAlignmentController _spectatorClientAlignmentController;
        private GameObject _worldRootMarker;
        private Transform _remoteAlignmentRoot;
        private RoleSelectView _roleSelectView;
        private LobbyView _lobbyHostView;
        private LobbyView _lobbyClientView;
        private LobbyView _lobbySpectatorView;
        private LobbyView _resultView;
        private M5PlayerHudView _playerHudView;
        private SpectatorControlView _spectatorControlView;
        private SpectatorBarrageView _spectatorBarrageView;
        private SpectatorAudioPlayer _spectatorAudioPlayer;
        private RemoteAlignmentController _spectatorWallPlacementController;
        private M3NetworkCoordinator _networkCoordinator;
        private M3RemotePlayerProxy _remoteProxy;
        private M3RemotePlayerProxy _spectatorHostProxy;
        private M3RemotePlayerProxy _spectatorClientProxy;
        private NetworkRole _selectedRole = NetworkRole.None;
        private Transform _worldRootTransform;
        private Transform _spectatorHostAlignmentRoot;
        private Transform _spectatorClientAlignmentRoot;
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
        private M5ShieldVisual _spectatorHostShieldVisual;
        private M5ShieldVisual _spectatorClientShieldVisual;

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
        private bool _spectatorClientAlignmentConfirmed;
        private bool _spectatorHostAlignmentConfirmed;
        private bool _localRematchReady;
        private bool _remoteRematchReady;
        private float _localSpectatorVoteCooldownUntil;
        private float _hostSpectatorVoteCooldownUntil;
        private readonly Dictionary<int, ObstacleStatePayload> _obstacleStates = new Dictionary<int, ObstacleStatePayload>();
        private readonly Dictionary<int, WallObstacleRuntime> _obstacleVisuals = new Dictionary<int, WallObstacleRuntime>();
        private Transform _obstacleVisualRoot;
        private WallObstacleRuntime _spectatorWallPreview;
        private bool _spectatorWallPlacementActive;
        private float _hostWallSpawnCooldownUntil;
        private float _hostObstacleStateBroadcastCooldownUntil;
        private int _nextObstacleId = 1;
        private UnityEngine.XR.InputDevice _wallPlacementLeftController;
        private UnityEngine.XR.InputDevice _wallPlacementRightController;
        private bool _wallPlacementLeftTriggerHeld;
        private bool _wallPlacementRightTriggerHeld;

        private bool IsAprilTagCalibrationActive => enableAutoAprilTagTracking && !disableAprilTagCalibrationTemporarily;

        private bool CanAdjustLiveRemoteAlignment()
        {
            if (IsAprilTagCalibrationActive)
            {
                return false;
            }

            return (_selectedRole == NetworkRole.Client && _liveCalibrationPhase == LiveCalibrationPhase.ClientAdjustHost) ||
                   (_selectedRole == NetworkRole.Host && _liveCalibrationPhase == LiveCalibrationPhase.HostAdjustClient) ||
                   (_selectedRole == NetworkRole.Spectator && _liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustClient) ||
                   (_selectedRole == NetworkRole.Spectator && _liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustHost);
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
            _roleSelectView = new RoleSelectView(camera.transform, OnHostSelected, OnClientSelected, OnSpectatorSelected, HandleRoleSelectBackClicked, menuDistance, menuVerticalOffset);
            hostIpForClient = LoadHostIpPreference(hostIpForClient);
            _lobbyHostView = new LobbyView(camera.transform, "Lobby Host", "Start Match", HandleHostStartMatchClicked, HandleLobbyBackClicked, menuDistance, menuVerticalOffset);
            _lobbyClientView = new LobbyView(camera.transform, "Lobby Client", "Connect", HandleClientConnectClicked, HandleLobbyBackClicked, menuDistance, menuVerticalOffset, OnHostIpEdited, "Host IP");
            _lobbySpectatorView = new LobbyView(camera.transform, "Lobby Spectator", "Connect", HandleSpectatorConnectClicked, HandleLobbyBackClicked, menuDistance, menuVerticalOffset, OnHostIpEdited, "Host IP");
            _calibrationView = new CalibrationView(camera.transform, HandleCalibrationConfirmClicked, HandleCalibrationBackClicked, menuDistance, menuVerticalOffset);
            _resultView = new LobbyView(camera.transform, "Result", "Retry", HandleResultRetryClicked, HandleResultBackToMenuClicked, menuDistance, menuVerticalOffset);
            _playerHudView = new M5PlayerHudView(camera.transform, hudDistance, hudLocalOffset);
            _spectatorControlView = new SpectatorControlView(
                camera.transform,
                spectatorPanelDistance,
                spectatorPanelLocalOffset,
                HandleSpectatorHealHostClicked,
                HandleSpectatorHealClientClicked,
                HandleSpectatorBarrageAClicked,
                HandleSpectatorBarrageBClicked,
                HandleSpectatorBarrageCClicked,
                HandleSpectatorCheerClicked,
                HandleSpectatorApplauseClicked,
                HandleSpectatorPlaceWallClicked);
            _spectatorBarrageView = new SpectatorBarrageView(camera.transform, spectatorBarrageDistance, spectatorBarrageLocalOffset);
            _spectatorAudioPlayer = new SpectatorAudioPlayer(transform);

            _worldRootTransform = EnsureWorldRootExists();
            _worldRootController = new WorldRootController(
                _worldRootTransform,
                camera.transform,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _remoteAlignmentRoot = EnsureRemoteAlignmentRootExists();
            _spectatorHostAlignmentRoot = EnsureNamedRootExists("SpectatorHostAlignmentRoot");
            _spectatorClientAlignmentRoot = EnsureNamedRootExists("SpectatorClientAlignmentRoot");
            _remoteAlignmentController = new RemoteAlignmentController(
                _remoteAlignmentRoot,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _spectatorHostAlignmentController = new RemoteAlignmentController(
                _spectatorHostAlignmentRoot,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _spectatorClientAlignmentController = new RemoteAlignmentController(
                _spectatorClientAlignmentRoot,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _obstacleVisualRoot = EnsureNamedRootExists("ObstacleVisualRoot");
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
            _stateMachine.Register(new LobbySpectatorState(_lobbySpectatorView, OnTickLobbySpectator));
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
            _networkCoordinator.RoleShootReceived += OnRoleShootReceived;
            _networkCoordinator.RoleShieldReceived += OnRoleShieldReceived;
            _networkCoordinator.HpUpdateReceived += OnHpUpdateReceived;
            _networkCoordinator.MatchResultReceived += OnMatchResultReceived;
            _networkCoordinator.SharedAnchorReceived += OnSharedAnchorReceived;
            _networkCoordinator.StartPlayingRequested += OnStartPlayingRequested;
            _networkCoordinator.RemoteCalibrationReadyReceived += OnRemoteCalibrationReadyReceived;
            _networkCoordinator.RemoteAlignmentReceived += OnRemoteAlignmentReceived;
            _networkCoordinator.RemoteRematchReadyReceived += OnRemoteRematchReadyReceived;
            _networkCoordinator.SpectatorVoteReceived += OnSpectatorVoteReceived;
            _networkCoordinator.ObstacleSpawnRequestReceived += OnObstacleSpawnRequestReceived;
            _networkCoordinator.ObstacleStateReceived += OnObstacleStateReceived;

            _remoteProxy = gameObject.GetComponent<M3RemotePlayerProxy>();
            if (_remoteProxy == null)
            {
                _remoteProxy = gameObject.AddComponent<M3RemotePlayerProxy>();
            }
            _remoteProxy.BindAlignmentRoot(_remoteAlignmentRoot);
            _remoteAlignmentController?.SetPivotTransform(_remoteProxy.HeadTransform);
            _spectatorHostProxy = CreateSpectatorProxy("SpectatorHostProxy", _spectatorHostAlignmentRoot);
            _spectatorClientProxy = CreateSpectatorProxy("SpectatorClientProxy", _spectatorClientAlignmentRoot);
            _spectatorHostProxy?.Hide();
            _spectatorClientProxy?.Hide();

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
            if (_selectedRole == NetworkRole.Spectator)
            {
                UpdateSpectatorVisuals();
            }
            else if (_networkCoordinator != null && _networkCoordinator.HasRemotePose && _remoteProxy != null)
            {
                _remoteProxy.ApplyPose(_networkCoordinator.LatestRemotePose);
            }

            _stateMachine?.Tick();
            TickCombat();
            RefreshObstacleVisuals();
            _playerHudView?.Tick();
            _spectatorControlView?.Tick();
            _spectatorBarrageView?.Tick(Time.deltaTime);
            UpdateSpectatorVoteUi();
            TickSpectatorWallPlacement();
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
                _networkCoordinator.RoleShootReceived -= OnRoleShootReceived;
                _networkCoordinator.RoleShieldReceived -= OnRoleShieldReceived;
                _networkCoordinator.HpUpdateReceived -= OnHpUpdateReceived;
                _networkCoordinator.MatchResultReceived -= OnMatchResultReceived;
                _networkCoordinator.SharedAnchorReceived -= OnSharedAnchorReceived;
                _networkCoordinator.StartPlayingRequested -= OnStartPlayingRequested;
                _networkCoordinator.RemoteCalibrationReadyReceived -= OnRemoteCalibrationReadyReceived;
                _networkCoordinator.RemoteAlignmentReceived -= OnRemoteAlignmentReceived;
                _networkCoordinator.RemoteRematchReadyReceived -= OnRemoteRematchReadyReceived;
                _networkCoordinator.SpectatorVoteReceived -= OnSpectatorVoteReceived;
                _networkCoordinator.ObstacleSpawnRequestReceived -= OnObstacleSpawnRequestReceived;
                _networkCoordinator.ObstacleStateReceived -= OnObstacleStateReceived;
                _networkCoordinator.Stop();
            }

            ClearObstacleVisuals();
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
            _stateMachine?.ChangeState(AppStateId.LobbyClient);
        }

        private void OnSpectatorSelected()
        {
            _selectedRole = NetworkRole.Spectator;
            _stateMachine?.ChangeState(AppStateId.LobbySpectator);
        }

        private void HandleClientConnectClicked()
        {
            if (_selectedRole != NetworkRole.Client || _networkCoordinator == null)
            {
                return;
            }

            hostIpForClient = SanitizeHostIp(_lobbyClientView != null ? _lobbyClientView.GetInputValue() : hostIpForClient);
            SaveHostIpPreference();
            _networkCoordinator.StartClient(hostIpForClient);
            if (enableSharedSpatialAnchor)
            {
                StartCoroutine(WarmupSpatialAnchorProviderRoutine());
            }
        }

        private void HandleSpectatorConnectClicked()
        {
            if (_selectedRole != NetworkRole.Spectator || _networkCoordinator == null)
            {
                return;
            }

            hostIpForClient = SanitizeHostIp(_lobbySpectatorView != null ? _lobbySpectatorView.GetInputValue() : hostIpForClient);
            SaveHostIpPreference();
            _networkCoordinator.StartSpectator(hostIpForClient);
        }

        private void OnHostIpEdited(string value)
        {
            hostIpForClient = SanitizeHostIp(value);
        }

        private void HandleLobbyBackClicked()
        {
            _networkCoordinator?.Stop();
            ClearObstacleVisuals();
            _obstacleStates.Clear();
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
            _remoteProxy?.Hide();
            _spectatorHostProxy?.Hide();
            _spectatorClientProxy?.Hide();
            _stateMachine?.ChangeState(AppStateId.RoleSelect);
        }

        private void OnTickLobbyHost()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            _lobbyHostView.SetStatus(_networkCoordinator.BuildLobbyStatus());
            var ready = _networkCoordinator.HasClientPeer && _networkCoordinator.HasSpectatorPeer;
            _lobbyHostView.SetPrimaryButton(ready ? "Start Match" : "Waiting All Roles...", ready);
        }

        private void OnTickLobbyClient()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            _lobbyClientView.SetInputVisible(true);
            if (string.IsNullOrWhiteSpace(_lobbyClientView.GetInputValue()))
            {
                _lobbyClientView.SetInputValue(hostIpForClient);
            }
            _lobbyClientView.SetStatus(_networkCoordinator.BuildLobbyStatus());
            _lobbyClientView.SetPrimaryButton(_networkCoordinator.IsConnected ? "Reconnect" : "Connect", true);
        }

        private void OnTickLobbySpectator()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            _lobbySpectatorView.SetInputVisible(true);
            if (string.IsNullOrWhiteSpace(_lobbySpectatorView.GetInputValue()))
            {
                _lobbySpectatorView.SetInputValue(hostIpForClient);
            }
            _lobbySpectatorView.SetStatus(_networkCoordinator.BuildLobbyStatus());
            _lobbySpectatorView.SetPrimaryButton(_networkCoordinator.IsConnected ? "Reconnect" : "Connect", true);

            if (_networkCoordinator.IsConnected &&
                _networkCoordinator.HasRemotePoseForRole(NetworkRole.Host) &&
                _networkCoordinator.HasRemotePoseForRole(NetworkRole.Client))
            {
                _stateMachine?.ChangeState(AppStateId.Calibration);
            }
        }

        private void HandleHostStartMatchClicked()
        {
            if (_selectedRole != NetworkRole.Host ||
                _networkCoordinator == null ||
                !_networkCoordinator.HasClientPeer ||
                !_networkCoordinator.HasSpectatorPeer)
            {
                return;
            }

            ResetCombatForNewMatch();
            _networkCoordinator.NotifyHostStartCalibration();
            _stateMachine?.ChangeState(AppStateId.Calibration);
        }

        private void OnRemoteCalibrationRequested()
        {
            if (_selectedRole == NetworkRole.Client || _selectedRole == NetworkRole.Spectator)
            {
                _stateMachine?.ChangeState(AppStateId.Calibration);
            }
        }

        private void HandleCalibrationConfirmClicked()
        {
            // 串行校准的确认入口：根据当前角色和阶段，只允许当前执行者推进到下一步。
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
                        _calibrationView?.SetStatus("Client step confirmed.\nWaiting for host to adjust Client avatar.");
                        return;
                    }

                    _calibrationView?.SetStatus(BuildLiveCalibrationWaitingStatus());
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
                        _liveCalibrationPhase = LiveCalibrationPhase.SpectatorAdjustClient;
                        _networkCoordinator?.NotifyRemoteAlignment(
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.position : Vector3.zero,
                            _remoteAlignmentRoot != null ? _remoteAlignmentRoot.rotation : Quaternion.identity,
                            true,
                            LiveCalibrationPhase.HostAdjustClient.ToString());
                        _calibrationView?.SetStatus("Host step confirmed.\nWaiting for spectator to adjust Client avatar.");
                        return;
                    }

                    if (_liveCalibrationPhase == LiveCalibrationPhase.HostFinalConfirm &&
                        (!_clientAlignmentConfirmed || !_hostAlignmentConfirmed || !_spectatorClientAlignmentConfirmed || !_spectatorHostAlignmentConfirmed))
                    {
                        _calibrationView?.SetStatus("Calibration steps incomplete.\nAll four adjustment steps must be confirmed.");
                        return;
                    }
                    else if (_liveCalibrationPhase != LiveCalibrationPhase.HostFinalConfirm)
                    {
                        _calibrationView?.SetStatus(BuildLiveCalibrationWaitingStatus());
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

            if (_selectedRole == NetworkRole.Spectator && !IsAprilTagCalibrationActive)
            {
                if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustClient)
                {
                    if (_networkCoordinator == null || !_networkCoordinator.HasRemotePoseForRole(NetworkRole.Client))
                    {
                        _calibrationView?.SetStatus("Waiting Client avatar stream before spectator client adjustment.");
                        return;
                    }

                    _spectatorClientAlignmentConfirmed = true;
                    _liveCalibrationPhase = LiveCalibrationPhase.SpectatorAdjustHost;
                    _networkCoordinator?.NotifyRemoteAlignment(
                        _spectatorClientAlignmentRoot != null ? _spectatorClientAlignmentRoot.position : Vector3.zero,
                        _spectatorClientAlignmentRoot != null ? _spectatorClientAlignmentRoot.rotation : Quaternion.identity,
                        true,
                        LiveCalibrationPhase.SpectatorAdjustClient.ToString());
                    _calibrationView?.SetStatus("Spectator client step confirmed.\nNow adjust Host avatar.");
                    return;
                }

                if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustHost)
                {
                    if (_networkCoordinator == null || !_networkCoordinator.HasRemotePoseForRole(NetworkRole.Host))
                    {
                        _calibrationView?.SetStatus("Waiting Host avatar stream before spectator host adjustment.");
                        return;
                    }

                    _spectatorHostAlignmentConfirmed = true;
                    _liveCalibrationPhase = LiveCalibrationPhase.HostFinalConfirm;
                    _networkCoordinator?.NotifyRemoteAlignment(
                        _spectatorHostAlignmentRoot != null ? _spectatorHostAlignmentRoot.position : Vector3.zero,
                        _spectatorHostAlignmentRoot != null ? _spectatorHostAlignmentRoot.rotation : Quaternion.identity,
                        true,
                        LiveCalibrationPhase.SpectatorAdjustHost.ToString());
                    _calibrationView?.SetStatus("Spectator host step confirmed.\nWaiting for host final Confirm.");
                    return;
                }

                _calibrationView?.SetStatus(BuildLiveCalibrationWaitingStatus());
                return;
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

            if (_selectedRole == NetworkRole.Spectator)
            {
                _stateMachine?.ChangeState(AppStateId.LobbySpectator);
                return;
            }

            _stateMachine?.ChangeState(AppStateId.MainMenu);
        }

        private void OnEnterCalibration()
        {
            // 每次进入校准都重置局部对齐状态、确认标记和可视化节点，避免上一局残留。
            _clientWorldRootLocked = false;
            _nextCalibrationSyncTime = 0f;
            _nextRemoteAlignmentSyncTime = 0f;
            _hasMarkerSample = false;
            _localCalibrationReady = false;
            _remoteCalibrationReady = false;
            _liveCalibrationPhase = IsAprilTagCalibrationActive ? LiveCalibrationPhase.HostFinalConfirm : LiveCalibrationPhase.ClientAdjustHost;
            _clientAlignmentConfirmed = false;
            _hostAlignmentConfirmed = false;
            _spectatorClientAlignmentConfirmed = false;
            _spectatorHostAlignmentConfirmed = false;
            _localCalibrationReadySince = -1f;
            _lastCalibrationReadySendTime = -999f;
            _lastSentCalibrationReady = false;
            if (_remoteAlignmentRoot != null)
            {
                _remoteAlignmentRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            if (_spectatorHostAlignmentRoot != null)
            {
                _spectatorHostAlignmentRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            if (_spectatorClientAlignmentRoot != null)
            {
                _spectatorClientAlignmentRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            _projectileShooter?.SetShootingEnabled(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _spectatorHostShieldVisual?.Deactivate();
            _spectatorClientShieldVisual?.Deactivate();
            _markerTrackingSource?.Begin();
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = true;
            }

            _calibrationView?.SetVisible(true);
            _calibrationView?.SetConfirmVisible(true);
            _calibrationView?.SetConfirmText(IsAprilTagCalibrationActive ? "Confirm" : "Confirm Step");
            if (_selectedRole == NetworkRole.Spectator)
            {
                _calibrationView?.SetStatus("Phase 1/5: Waiting for Client to adjust Host avatar.");
                _calibrationView?.SetDetectionStatus("<color=#6CA9D9>Five-step serial calibration. Only the active device can adjust each phase.</color>");
            }
            else if (_selectedRole == NetworkRole.Client)
            {
                _calibrationView?.SetStatus(IsAprilTagCalibrationActive
                    ? "Waiting host confirmation...\nAuto AprilTag localization in progress."
                    : "Phase 1/5: Client adjusts Host avatar.\nFine-tune, then press Confirm Step.");
                _calibrationView?.SetDetectionStatus(IsAprilTagCalibrationActive
                    ? "<color=#6CA9D9>Detection: searching AprilTag automatically...</color>"
                    : "<color=#6CA9D9>Detection: AprilTag disabled. Live remote alignment only.</color>");
            }
            else
            {
                _calibrationView?.SetStatus(IsAprilTagCalibrationActive
                    ? (_worldRootController != null ? _worldRootController.BuildStatusText() : "WorldRoot unavailable")
                    : "Phase 1/5: Waiting for Client to adjust Host avatar.");
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
            _spectatorHostShieldVisual?.Deactivate();
            _spectatorClientShieldVisual?.Deactivate();
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

            if (!IsAprilTagCalibrationActive && CanAdjustLiveRemoteAlignment())
            {
                if (_selectedRole == NetworkRole.Client && _networkCoordinator != null && _networkCoordinator.HasRemotePose)
                {
                    _remoteAlignmentController?.Tick(Time.deltaTime);
                }
                else if (_selectedRole == NetworkRole.Host && _networkCoordinator != null && _networkCoordinator.HasRemotePose)
                {
                    _remoteAlignmentController?.Tick(Time.deltaTime);
                }
                else if (_selectedRole == NetworkRole.Spectator)
                {
                    if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustClient &&
                        _networkCoordinator != null &&
                        _networkCoordinator.HasRemotePoseForRole(NetworkRole.Client))
                    {
                        _spectatorClientAlignmentController?.Tick(Time.deltaTime);
                    }
                    else if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustHost &&
                             _networkCoordinator != null &&
                             _networkCoordinator.HasRemotePoseForRole(NetworkRole.Host))
                    {
                        _spectatorHostAlignmentController?.Tick(Time.deltaTime);
                    }
                }
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
                _calibrationView?.SetConfirmText(_liveCalibrationPhase == LiveCalibrationPhase.HostFinalConfirm ? "Confirm" : "Confirm Step");
                _calibrationView?.SetStatus($"Live remote alignment mode.\n{BuildLiveCalibrationPhaseStatus()}\n{BuildRemoteAlignmentStatusText()}");
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
            if (_selectedRole == NetworkRole.Spectator)
            {
                var hostReady = _networkCoordinator != null && _networkCoordinator.HasRemotePoseForRole(NetworkRole.Host);
                var clientReady = _networkCoordinator != null && _networkCoordinator.HasRemotePoseForRole(NetworkRole.Client);
                return $"<color=#6CA9D9>Spectator local calibration.</color>\nHost Stream: {(hostReady ? "<color=#7CFF9A>READY</color>" : "<color=#FF8A8A>WAIT</color>")} / Client Stream: {(clientReady ? "<color=#7CFF9A>READY</color>" : "<color=#FF8A8A>WAIT</color>")}";
            }

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
            if (_selectedRole == NetworkRole.Spectator)
            {
                return BuildSpectatorAlignmentStatusText();
            }

            if (_networkCoordinator == null || !_networkCoordinator.HasRemotePose || _remoteAlignmentController == null)
            {
                return "Remote refine: waiting remote avatar";
            }

            return _remoteAlignmentController.BuildStatusText();
        }

        private void UpdateLocalCalibrationReady(float now)
        {
            if (_selectedRole == NetworkRole.Spectator)
            {
                _localCalibrationReady = _spectatorClientAlignmentConfirmed && _spectatorHostAlignmentConfirmed;
                _remoteCalibrationReady = false;
                _localCalibrationReadySince = _localCalibrationReady ? now : -1f;
                return;
            }

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
                else if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustClient)
                {
                    _localCalibrationReady = _selectedRole == NetworkRole.Spectator
                        ? (_networkCoordinator != null && _networkCoordinator.HasRemotePoseForRole(NetworkRole.Client))
                        : _spectatorClientAlignmentConfirmed;
                }
                else if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustHost)
                {
                    _localCalibrationReady = _selectedRole == NetworkRole.Spectator
                        ? (_networkCoordinator != null && _networkCoordinator.HasRemotePoseForRole(NetworkRole.Host))
                        : _spectatorHostAlignmentConfirmed;
                }
                else
                {
                    _localCalibrationReady = _clientAlignmentConfirmed && _hostAlignmentConfirmed && _spectatorClientAlignmentConfirmed && _spectatorHostAlignmentConfirmed;
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
            if (_selectedRole == NetworkRole.Spectator)
            {
                return;
            }

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
            if (_selectedRole == NetworkRole.Host || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.CurrentId == AppStateId.Result)
            {
                ResetCombatForNewMatch();
                _stateMachine.ChangeState(AppStateId.Playing);
                return;
            }

            if (_stateMachine.CurrentId == AppStateId.Calibration ||
                _stateMachine.CurrentId == AppStateId.LobbyClient ||
                _stateMachine.CurrentId == AppStateId.LobbySpectator)
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
            // 这里负责把远端设备发来的“该步骤已确认”消息转换成当前设备的阶段推进。
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
                _liveCalibrationPhase = LiveCalibrationPhase.SpectatorAdjustClient;
                return;
            }

            if (payload.senderRole == NetworkRole.Spectator.ToString() &&
                payload.stage == LiveCalibrationPhase.SpectatorAdjustClient.ToString() &&
                payload.confirmed)
            {
                _spectatorClientAlignmentConfirmed = true;
                _liveCalibrationPhase = LiveCalibrationPhase.SpectatorAdjustHost;
                return;
            }

            if (payload.senderRole == NetworkRole.Spectator.ToString() &&
                payload.stage == LiveCalibrationPhase.SpectatorAdjustHost.ToString() &&
                payload.confirmed)
            {
                _spectatorHostAlignmentConfirmed = true;
                _liveCalibrationPhase = LiveCalibrationPhase.HostFinalConfirm;
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

        private void OnRoleShootReceived(NetworkRole senderRole, ShootPayload shot)
        {
            if (_projectileShooter == null ||
                shot == null ||
                senderRole == NetworkRole.None ||
                senderRole == NetworkRole.Spectator ||
                _stateMachine == null ||
                _stateMachine.CurrentId != AppStateId.Playing)
            {
                return;
            }

            var visualSpawn = TransformRolePositionForDisplay(senderRole, shot.spawnPosition);
            var visualDirection = TransformRoleDirectionForDisplay(senderRole, shot.direction);
            _projectileShooter.SpawnRemoteProjectile(
                visualSpawn,
                visualDirection,
                shot.speed,
                shot.maxDistance,
                shot.lifetime);

            if (_selectedRole == NetworkRole.Host && senderRole == NetworkRole.Client)
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
            CancelSpectatorWallPlacement();
            if (_hostHp <= 0 || _clientHp <= 0)
            {
                _hostHp = GetMaxHp();
                _clientHp = GetMaxHp();
            }

            var canFight = _selectedRole == NetworkRole.Host || _selectedRole == NetworkRole.Client;
            _projectileShooter?.SetShootingEnabled(canFight);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = canFight;
            }

            EnsureShieldVisuals();
            BindShieldAnchors();
            _playerHudView?.SetVisible(canFight);
            _spectatorControlView?.SetVisible(_selectedRole == NetworkRole.Spectator);
            _spectatorBarrageView?.SetVisible(_selectedRole == NetworkRole.Spectator);
            if (_selectedRole == NetworkRole.Spectator)
            {
                _remoteProxy?.Hide();
            }
            else
            {
                _spectatorHostProxy?.Hide();
                _spectatorClientProxy?.Hide();
            }
            RefreshRayVisuals();
            Debug.Log($"M5: Enter Playing as {_selectedRole}. HostHP={_hostHp} ClientHP={_clientHp}");
        }

        private void OnExitPlaying()
        {
            CancelSpectatorWallPlacement();
            _projectileShooter?.SetShootingEnabled(false);
            if (_alwaysVisibleLaser != null)
            {
                _alwaysVisibleLaser.enabled = false;
            }

            _calibrationView?.SetVisible(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _spectatorHostShieldVisual?.Deactivate();
            _spectatorClientShieldVisual?.Deactivate();
            _playerHudView?.SetVisible(false);
            _spectatorControlView?.SetVisible(false);
            _spectatorBarrageView?.SetVisible(false);
            if (_selectedRole != NetworkRole.Spectator)
            {
                _spectatorHostProxy?.Hide();
                _spectatorClientProxy?.Hide();
            }
        }

        private void OnEnterResult()
        {
            CancelSpectatorWallPlacement();
            _projectileShooter?.SetShootingEnabled(false);
            _localShieldVisual?.Deactivate();
            _remoteShieldVisual?.Deactivate();
            _spectatorHostShieldVisual?.Deactivate();
            _spectatorClientShieldVisual?.Deactivate();
            _localRematchReady = false;
            _remoteRematchReady = false;
            _resultView?.SetVisible(true);
            var canRematch = _selectedRole == NetworkRole.Host || _selectedRole == NetworkRole.Client;
            _resultView?.SetStatus(canRematch
                ? _resultText + $"\n[{BuildStamp}]\n\nPress Retry. Both players must confirm to rematch."
                : _resultText + $"\n[{BuildStamp}]");
            _resultView?.SetPrimaryButton(canRematch ? "Retry" : "Observe", canRematch);
            _playerHudView?.SetVisible(canRematch);
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
            ClearObstacleVisuals();
            _obstacleStates.Clear();
            _selectedRole = NetworkRole.None;
            _clientWorldRootLocked = false;
            _stateMachine?.ChangeState(AppStateId.MainMenu);
        }

        private void HandleSpectatorHealHostClicked()
        {
            TrySubmitSpectatorVote(NetworkRole.Host);
        }

        private void HandleSpectatorHealClientClicked()
        {
            TrySubmitSpectatorVote(NetworkRole.Client);
        }

        private void HandleSpectatorBarrageAClicked()
        {
            ShowLocalSpectatorBarrage(GetSpectatorBarrageWordA());
        }

        private void HandleSpectatorBarrageBClicked()
        {
            ShowLocalSpectatorBarrage(GetSpectatorBarrageWordB());
        }

        private void HandleSpectatorBarrageCClicked()
        {
            ShowLocalSpectatorBarrage(GetSpectatorBarrageWordC());
        }

        private void HandleSpectatorCheerClicked()
        {
            _spectatorAudioPlayer?.Play(GetSpectatorCheerClip(), GetSpectatorAudioVolume());
        }

        private void HandleSpectatorApplauseClicked()
        {
            _spectatorAudioPlayer?.Play(GetSpectatorApplauseClip(), GetSpectatorAudioVolume());
        }

        private void HandleSpectatorPlaceWallClicked()
        {
            if (_selectedRole != NetworkRole.Spectator || _stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing)
            {
                return;
            }

            if (_spectatorWallPlacementActive)
            {
                return;
            }

            if (!TryEnterSpectatorWallPlacement())
            {
                _spectatorControlView?.SetStatus("Place Wall unavailable.\nNeed both Host and Client tracked before obstacle preview.");
            }
        }

        private void TrySubmitSpectatorVote(NetworkRole targetRole)
        {
            if (_selectedRole != NetworkRole.Spectator || _networkCoordinator == null || !_networkCoordinator.IsConnected)
            {
                return;
            }

            var now = Time.time;
            if (now < _localSpectatorVoteCooldownUntil)
            {
                return;
            }

            _localSpectatorVoteCooldownUntil = now + GetSpectatorVoteCooldown();
            _networkCoordinator.NotifySpectatorVote(targetRole.ToString());
        }

        private void ShowLocalSpectatorBarrage(string message)
        {
            if (_selectedRole != NetworkRole.Spectator || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _spectatorBarrageView?.ShowMessage(message, GetSpectatorBarrageDuration(), GetSpectatorBarrageSpeed());
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

        private void OnRoleShieldReceived(NetworkRole senderRole, ShieldPayload payload)
        {
            if (payload == null || !payload.active || senderRole == NetworkRole.None || senderRole == NetworkRole.Spectator)
            {
                return;
            }

            if (_selectedRole == NetworkRole.Host && senderRole == NetworkRole.Client)
            {
                ActivateClientShieldAuthoritative(payload.duration);
            }
            else if (_selectedRole == NetworkRole.Client && senderRole == NetworkRole.Host)
            {
                ActivateHostShieldVisual(payload.duration);
            }
            else if (_selectedRole == NetworkRole.Spectator)
            {
                ActivateSpectatorShieldVisual(senderRole, payload.duration);
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
            if (_selectedRole == NetworkRole.Spectator)
            {
                var maxHp = Mathf.Max(1, GetMaxHp());
                _spectatorHostProxy?.SetEnemyHealthNormalized(Mathf.Clamp01(_hostHp / (float)maxHp));
                _spectatorClientProxy?.SetEnemyHealthNormalized(Mathf.Clamp01(_clientHp / (float)maxHp));
            }
            Debug.Log($"M5: HP update => Host={_hostHp} Client={_clientHp}");
        }

        private void OnSpectatorVoteReceived(SpectatorVotePayload payload)
        {
            if (_selectedRole != NetworkRole.Host || payload == null || string.IsNullOrEmpty(payload.targetRole))
            {
                return;
            }

            var now = Time.time;
            if (now < _hostSpectatorVoteCooldownUntil)
            {
                return;
            }

            var target = payload.targetRole;
            if (string.Equals(target, NetworkRole.Host.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _hostHp = Mathf.Min(GetMaxHp(), _hostHp + GetSpectatorHealAmount());
            }
            else if (string.Equals(target, NetworkRole.Client.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _clientHp = Mathf.Min(GetMaxHp(), _clientHp + GetSpectatorHealAmount());
            }
            else
            {
                return;
            }

            _hostSpectatorVoteCooldownUntil = now + GetSpectatorVoteCooldown();
            _networkCoordinator?.NotifyHostHpUpdate(_hostHp, _clientHp);
            UpdateEnemyHealthBar();
        }

        private void OnObstacleSpawnRequestReceived(ObstacleSpawnRequestPayload payload)
        {
            if (_selectedRole != NetworkRole.Host ||
                payload == null ||
                string.IsNullOrEmpty(payload.anchorType) ||
                _stateMachine == null ||
                _stateMachine.CurrentId != AppStateId.Playing)
            {
                return;
            }

            TrySpawnHostObstacleFromRequest(payload);
        }

        private void OnObstacleStateReceived(ObstacleStatePayload payload)
        {
            if (payload == null || _selectedRole == NetworkRole.Host)
            {
                return;
            }

            ApplyObstacleState(payload);
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
            _resultText = _selectedRole == NetworkRole.Spectator
                ? $"{winner.ToUpperInvariant()} WIN"
                : (localWon ? "WIN" : "LOSE");
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
            TickObstacleStates(now);
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
            _localSpectatorVoteCooldownUntil = 0f;
            _hostSpectatorVoteCooldownUntil = 0f;
            _hostWallSpawnCooldownUntil = 0f;
            _hostObstacleStateBroadcastCooldownUntil = 0f;
            _resultText = "Result";
            ResetObstaclesForNewMatch();
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

            if (TryResolveObstacleHit(shot))
            {
                Debug.Log("M6: Host shot blocked by wall");
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

            if (TryResolveObstacleHit(shot))
            {
                Debug.Log("M6: Client shot blocked by wall");
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

        private void UpdateSpectatorVisuals()
        {
            if (_networkCoordinator == null)
            {
                return;
            }

            if (_networkCoordinator.TryGetRemotePose(NetworkRole.Host, out var hostPose))
            {
                _spectatorHostProxy?.ApplyPose(hostPose);
                _spectatorHostAlignmentController?.SetPivotTransform(_spectatorHostProxy != null ? _spectatorHostProxy.HeadTransform : null);
            }

            if (_networkCoordinator.TryGetRemotePose(NetworkRole.Client, out var clientPose))
            {
                _spectatorClientProxy?.ApplyPose(clientPose);
                _spectatorClientAlignmentController?.SetPivotTransform(_spectatorClientProxy != null ? _spectatorClientProxy.HeadTransform : null);
            }
        }

        private void TickSpectatorCalibration()
        {
            UpdateSpectatorVisuals();
            _calibrationView?.SetConfirmVisible(true);
            _calibrationView?.SetDetectionStatus(BuildCalibrationDetectionText());
            _calibrationView?.SetConfirmText(_liveCalibrationPhase == LiveCalibrationPhase.HostFinalConfirm ? "Confirm" : "Confirm Step");
            _calibrationView?.SetStatus($"Live remote alignment mode.\n{BuildLiveCalibrationPhaseStatus()}\n{BuildRemoteAlignmentStatusText()}");
        }

        private string BuildSpectatorAlignmentStatusText()
        {
            RemoteAlignmentController controller = null;
            string label = string.Empty;
            if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustHost)
            {
                controller = _spectatorHostAlignmentController;
                label = "Spectator -> Host";
            }
            else if (_liveCalibrationPhase == LiveCalibrationPhase.SpectatorAdjustClient)
            {
                controller = _spectatorClientAlignmentController;
                label = "Spectator -> Client";
            }

            if (controller == null)
            {
                return "Spectator offset: ready";
            }

            return $"Target: {label}\n{controller.BuildStatusText()}";
        }

        private string BuildLiveCalibrationPhaseStatus()
        {
            switch (_liveCalibrationPhase)
            {
                case LiveCalibrationPhase.ClientAdjustHost:
                    return _selectedRole == NetworkRole.Client
                        ? "Phase 1/5: Client adjusts Host avatar.\nOnly Client can adjust. Press Confirm Step when done."
                        : "Phase 1/5: Waiting for Client to adjust Host avatar.";
                case LiveCalibrationPhase.HostAdjustClient:
                    return _selectedRole == NetworkRole.Host
                        ? "Phase 2/5: Host adjusts Client avatar.\nOnly Host can adjust. Press Confirm Step when done."
                        : "Phase 2/5: Waiting for Host to adjust Client avatar.";
                case LiveCalibrationPhase.SpectatorAdjustClient:
                    return _selectedRole == NetworkRole.Spectator
                        ? "Phase 3/5: Spectator adjusts Client avatar.\nOnly Spectator can adjust. Press Confirm Step when done."
                        : "Phase 3/5: Waiting for Spectator to adjust Client avatar.";
                case LiveCalibrationPhase.SpectatorAdjustHost:
                    return _selectedRole == NetworkRole.Spectator
                        ? "Phase 4/5: Spectator adjusts Host avatar.\nOnly Spectator can adjust. Press Confirm Step when done."
                        : "Phase 4/5: Waiting for Spectator to adjust Host avatar.";
                default:
                    return _selectedRole == NetworkRole.Host
                        ? "Phase 5/5: All four steps confirmed.\nOnly Host can press Confirm to start."
                        : "Phase 5/5: Waiting for Host final Confirm.";
            }
        }

        private string BuildLiveCalibrationWaitingStatus()
        {
            return BuildLiveCalibrationPhaseStatus();
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

        private Vector3 TransformRolePositionForDisplay(NetworkRole senderRole, Vector3 rawPosition)
        {
            var root = GetAlignmentRootForRole(senderRole);
            return root != null ? root.TransformPoint(rawPosition) : rawPosition;
        }

        private Vector3 TransformRoleDirectionForDisplay(NetworkRole senderRole, Vector3 rawDirection)
        {
            var dir = rawDirection.sqrMagnitude < 0.0001f ? Vector3.forward : rawDirection.normalized;
            var root = GetAlignmentRootForRole(senderRole);
            return root != null ? (root.rotation * dir).normalized : dir;
        }

        private Vector3 TransformObstaclePositionForDisplay(Vector3 rawPosition)
        {
            if (_selectedRole == NetworkRole.Host)
            {
                return rawPosition;
            }

            if (_selectedRole == NetworkRole.Client)
            {
                return TransformRemotePositionForDisplay(rawPosition);
            }

            if (_selectedRole == NetworkRole.Spectator &&
                TryGetArenaWorldBasis(out var rawCenter, out var rawForward, out var rawRight, out _) &&
                TryGetArenaDisplayBasis(out var displayCenter, out var displayForward, out var displayRight, out _))
            {
                var delta = rawPosition - rawCenter;
                var localX = Vector3.Dot(delta, rawRight);
                var localY = delta.y;
                var localZ = Vector3.Dot(delta, rawForward);
                return displayCenter + displayRight * localX + Vector3.up * localY + displayForward * localZ;
            }

            return rawPosition;
        }

        private Quaternion TransformObstacleRotationForDisplay(Quaternion rawRotation)
        {
            if (_selectedRole == NetworkRole.Host)
            {
                return rawRotation;
            }

            if (_selectedRole == NetworkRole.Client)
            {
                return _remoteAlignmentRoot != null ? _remoteAlignmentRoot.rotation * rawRotation : rawRotation;
            }

            if (_selectedRole == NetworkRole.Spectator &&
                TryGetArenaWorldBasis(out _, out _, out _, out var rawBaseYaw) &&
                TryGetArenaDisplayBasis(out _, out _, out _, out var displayBaseYaw))
            {
                var yawOffset = Mathf.DeltaAngle(rawBaseYaw, rawRotation.eulerAngles.y);
                return Quaternion.Euler(0f, displayBaseYaw + yawOffset, 0f);
            }

            return rawRotation;
        }

        private Transform GetAlignmentRootForRole(NetworkRole senderRole)
        {
            if (_selectedRole == NetworkRole.Spectator)
            {
                if (senderRole == NetworkRole.Host)
                {
                    return _spectatorHostAlignmentRoot;
                }

                if (senderRole == NetworkRole.Client)
                {
                    return _spectatorClientAlignmentRoot;
                }
            }

            return _remoteAlignmentRoot;
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

        private void ActivateSpectatorShieldVisual(NetworkRole senderRole, float duration)
        {
            var d = Mathf.Max(0.1f, duration);
            if (senderRole == NetworkRole.Host)
            {
                _hostShieldEndTime = Time.time + d;
                _spectatorHostShieldVisual?.Activate(d);
            }
            else if (senderRole == NetworkRole.Client)
            {
                _clientShieldEndTime = Time.time + d;
                _spectatorClientShieldVisual?.Activate(d);
            }
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

            if (_spectatorHostShieldVisual == null)
            {
                var hostShieldAnchor = transform.Find("SpectatorHostShieldRoot");
                if (hostShieldAnchor == null)
                {
                    var rootGo = new GameObject("SpectatorHostShieldRoot");
                    rootGo.transform.SetParent(transform, false);
                    hostShieldAnchor = rootGo.transform;
                }

                _spectatorHostShieldVisual = hostShieldAnchor.GetComponent<M5ShieldVisual>();
                if (_spectatorHostShieldVisual == null)
                {
                    _spectatorHostShieldVisual = hostShieldAnchor.gameObject.AddComponent<M5ShieldVisual>();
                }
            }

            if (_spectatorClientShieldVisual == null)
            {
                var clientShieldAnchor = transform.Find("SpectatorClientShieldRoot");
                if (clientShieldAnchor == null)
                {
                    var rootGo = new GameObject("SpectatorClientShieldRoot");
                    rootGo.transform.SetParent(transform, false);
                    clientShieldAnchor = rootGo.transform;
                }

                _spectatorClientShieldVisual = clientShieldAnchor.GetComponent<M5ShieldVisual>();
                if (_spectatorClientShieldVisual == null)
                {
                    _spectatorClientShieldVisual = clientShieldAnchor.gameObject.AddComponent<M5ShieldVisual>();
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

            if (_spectatorHostShieldVisual != null && _spectatorHostProxy != null && _spectatorHostProxy.HeadTransform != null)
            {
                _spectatorHostShieldVisual.BindAnchor(_spectatorHostProxy.HeadTransform);
            }

            if (_spectatorClientShieldVisual != null && _spectatorClientProxy != null && _spectatorClientProxy.HeadTransform != null)
            {
                _spectatorClientShieldVisual.BindAnchor(_spectatorClientProxy.HeadTransform);
            }
        }

        private bool TryEnterSpectatorWallPlacement()
        {
            if (_selectedRole != NetworkRole.Spectator)
            {
                return false;
            }

            if (!TryGetArenaDisplayBasis(out var displayCenter, out var displayForward, out _, out var displayBaseYaw))
            {
                return false;
            }

            CancelSpectatorWallPlacement();

            _spectatorWallPreview = new WallObstacleRuntime("SpectatorWallPreview", -1, _obstacleVisualRoot, GetWallSize(), isPreview: true);
            _spectatorWallPlacementController = new RemoteAlignmentController(
                _spectatorWallPreview.Transform,
                calibrationMoveSpeed,
                calibrationRotateSpeed,
                calibrationHeightSpeed);
            _spectatorWallPlacementController.SetPivotTransform(_spectatorWallPreview.Transform);

            var forward = Vector3.ProjectOnPlane(Camera.main != null ? Camera.main.transform.forward : displayForward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = displayForward;
            }

            var initialPosition = displayCenter + forward * GetWallPlacementDistance();
            initialPosition.y = displayCenter.y;
            _spectatorWallPreview.SetTransform(initialPosition, Quaternion.Euler(0f, displayBaseYaw, 0f));
            _spectatorWallPreview.SetPreviewVisible(true);
            _spectatorWallPlacementActive = true;
            _wallPlacementLeftTriggerHeld = false;
            _wallPlacementRightTriggerHeld = false;
            return true;
        }

        private void CancelSpectatorWallPlacement()
        {
            _spectatorWallPlacementActive = false;
            _wallPlacementLeftTriggerHeld = false;
            _wallPlacementRightTriggerHeld = false;
            _spectatorWallPlacementController = null;
            if (_spectatorWallPreview != null)
            {
                _spectatorWallPreview.Dispose();
                _spectatorWallPreview = null;
            }
        }

        private void TickSpectatorWallPlacement()
        {
            if (!_spectatorWallPlacementActive || _selectedRole != NetworkRole.Spectator || _stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing)
            {
                return;
            }

            _spectatorWallPlacementController?.Tick(Time.deltaTime);
            _spectatorControlView?.SetStatus(
                "Placement Preview\nRight Stick: Move XZ / Hold X or Y: Rotate / A/B: Height\nRight Trigger: Confirm Wall / Left Trigger: Cancel");

            EnsureWallPlacementDevices();

            var rightTriggerPressed = TryReadTriggerButton(_wallPlacementRightController);
            var leftTriggerPressed = TryReadTriggerButton(_wallPlacementLeftController);

            if (rightTriggerPressed && !_wallPlacementRightTriggerHeld)
            {
                ConfirmSpectatorWallPlacement();
            }
            else if (leftTriggerPressed && !_wallPlacementLeftTriggerHeld)
            {
                CancelSpectatorWallPlacement();
            }

            _wallPlacementRightTriggerHeld = rightTriggerPressed;
            _wallPlacementLeftTriggerHeld = leftTriggerPressed;
        }

        private void ConfirmSpectatorWallPlacement()
        {
            if (_spectatorWallPreview == null ||
                _networkCoordinator == null ||
                !_networkCoordinator.IsConnected ||
                !TryGetArenaDisplayBasis(out var displayCenter, out var displayForward, out var displayRight, out var displayBaseYaw))
            {
                CancelSpectatorWallPlacement();
                return;
            }

            var delta = _spectatorWallPreview.Transform.position - displayCenter;
            var localOffset = new Vector3(
                Vector3.Dot(delta, displayRight),
                delta.y,
                Vector3.Dot(delta, displayForward));
            var yawOffset = Mathf.DeltaAngle(displayBaseYaw, _spectatorWallPreview.Transform.eulerAngles.y);
            _networkCoordinator.NotifyObstacleSpawnRequest(ObstacleArenaAnchorType, localOffset, yawOffset);
            CancelSpectatorWallPlacement();
        }

        private void EnsureWallPlacementDevices()
        {
            if (!_wallPlacementLeftController.isValid)
            {
                _wallPlacementLeftController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            }

            if (!_wallPlacementRightController.isValid)
            {
                _wallPlacementRightController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            }
        }

        private static bool TryReadTriggerButton(UnityEngine.XR.InputDevice device)
        {
            if (!device.isValid)
            {
                return false;
            }

            var pressed = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var triggerButton) && triggerButton;
            if (pressed)
            {
                return true;
            }

            var axis = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out var triggerAxis) ? triggerAxis : 0f;
            return axis > 0.25f;
        }

        private void TickObstacleStates(float now)
        {
            if (_selectedRole != NetworkRole.Host || _obstacleStates.Count == 0)
            {
                return;
            }

            var obstacleIds = new List<int>();
            obstacleIds.AddRange(_obstacleStates.Keys);
            var changed = false;
            for (var index = 0; index < obstacleIds.Count; index++)
            {
                var obstacleId = obstacleIds[index];
                if (!_obstacleStates.TryGetValue(obstacleId, out var payload) || payload == null || !payload.active)
                {
                    continue;
                }

                payload.currentHp = Mathf.Max(0f, payload.currentHp - GetWallDecayPerSecond() * Time.deltaTime);
                ApplyObstacleState(payload);
                changed = true;
                if (payload.currentHp <= 0.001f)
                {
                    RemoveObstacleState(obstacleId, broadcastIfHost: true);
                }
            }

            if (changed && now >= _hostObstacleStateBroadcastCooldownUntil)
            {
                BroadcastAllObstacleStates();
                _hostObstacleStateBroadcastCooldownUntil = now + 0.1f;
            }
        }

        private void TrySpawnHostObstacleFromRequest(ObstacleSpawnRequestPayload payload)
        {
            if (_selectedRole != NetworkRole.Host ||
                payload == null ||
                payload.anchorType != ObstacleArenaAnchorType ||
                Time.time < _hostWallSpawnCooldownUntil ||
                GetActiveObstacleCount() >= GetWallMaxActiveCount() ||
                !TryGetArenaWorldBasis(out var center, out var forward, out var right, out var baseYaw))
            {
                return;
            }

            var worldPosition = center + right * payload.localOffset.x + Vector3.up * payload.localOffset.y + forward * payload.localOffset.z;
            var worldRotation = Quaternion.Euler(0f, baseYaw + payload.yawOffset, 0f);
            var obstacleId = _nextObstacleId++;
            var state = new ObstacleStatePayload
            {
                obstacleId = obstacleId,
                position = worldPosition,
                rotation = worldRotation,
                size = GetWallSize(),
                currentHp = GetWallMaxHp(),
                maxHp = GetWallMaxHp(),
                active = true
            };

            _hostWallSpawnCooldownUntil = Time.time + GetWallSpawnCooldown();
            ApplyObstacleState(state);
            BroadcastObstacleState(state);
        }

        private bool TryResolveObstacleHit(M1ProjectileShooter.ShotInfo shot)
        {
            if (_selectedRole != NetworkRole.Host || _obstacleVisuals.Count == 0)
            {
                return false;
            }

            var ray = new Ray(shot.spawnPosition, shot.direction.sqrMagnitude < 0.0001f ? Vector3.forward : shot.direction.normalized);
            var maxDistance = Mathf.Max(0.1f, shot.maxDistance);
            var closestDistance = float.MaxValue;
            var closestObstacleId = -1;
            var obstacleIds = new List<int>();
            obstacleIds.AddRange(_obstacleVisuals.Keys);
            for (var index = 0; index < obstacleIds.Count; index++)
            {
                var obstacleId = obstacleIds[index];
                if (!_obstacleVisuals.TryGetValue(obstacleId, out var obstacleRuntime) ||
                    obstacleRuntime == null ||
                    obstacleRuntime.Collider == null ||
                    !_obstacleStates.TryGetValue(obstacleId, out var obstacleState) ||
                    obstacleState == null ||
                    !obstacleState.active)
                {
                    continue;
                }

                if (obstacleRuntime.Collider.Raycast(ray, out var hitInfo, maxDistance) && hitInfo.distance < closestDistance)
                {
                    closestDistance = hitInfo.distance;
                    closestObstacleId = obstacleId;
                }
            }

            if (closestObstacleId < 0)
            {
                return false;
            }

            ApplyObstacleDamage(closestObstacleId, GetWallShotDamage());
            return true;
        }

        private void ApplyObstacleDamage(int obstacleId, float damage)
        {
            if (_selectedRole != NetworkRole.Host ||
                !_obstacleStates.TryGetValue(obstacleId, out var payload) ||
                payload == null ||
                !payload.active)
            {
                return;
            }

            payload.currentHp = Mathf.Max(0f, payload.currentHp - Mathf.Max(0f, damage));
            ApplyObstacleState(payload);
            if (payload.currentHp <= 0.001f)
            {
                RemoveObstacleState(obstacleId, broadcastIfHost: true);
                return;
            }

            BroadcastObstacleState(payload);
        }

        private void ApplyObstacleState(ObstacleStatePayload payload)
        {
            if (payload == null)
            {
                return;
            }

            if (!payload.active || payload.currentHp <= 0.001f)
            {
                RemoveObstacleState(payload.obstacleId, broadcastIfHost: false);
                return;
            }

            _obstacleStates[payload.obstacleId] = payload;
            var runtime = GetOrCreateObstacleVisual(payload);
            runtime.SetHp(payload.currentHp, payload.maxHp);
            runtime.SetTransform(TransformObstaclePositionForDisplay(payload.position), TransformObstacleRotationForDisplay(payload.rotation));
        }

        private void RemoveObstacleState(int obstacleId, bool broadcastIfHost)
        {
            if (_obstacleStates.ContainsKey(obstacleId))
            {
                _obstacleStates.Remove(obstacleId);
            }

            if (_obstacleVisuals.TryGetValue(obstacleId, out var runtime) && runtime != null)
            {
                runtime.Dispose();
            }

            _obstacleVisuals.Remove(obstacleId);

            if (broadcastIfHost && _selectedRole == NetworkRole.Host)
            {
                _networkCoordinator?.NotifyHostObstacleState(obstacleId, Vector3.zero, Quaternion.identity, GetWallSize(), 0f, GetWallMaxHp(), false);
            }
        }

        private void RefreshObstacleVisuals()
        {
            if (_obstacleStates.Count == 0)
            {
                return;
            }

            var ids = new List<int>();
            ids.AddRange(_obstacleStates.Keys);
            for (var index = 0; index < ids.Count; index++)
            {
                var obstacleId = ids[index];
                if (!_obstacleStates.TryGetValue(obstacleId, out var payload) || payload == null || !payload.active)
                {
                    continue;
                }

                var runtime = GetOrCreateObstacleVisual(payload);
                runtime.SetTransform(TransformObstaclePositionForDisplay(payload.position), TransformObstacleRotationForDisplay(payload.rotation));
                if (Camera.main != null)
                {
                    runtime.LookHpBarAt(Camera.main.transform.position);
                }
            }

        }

        private WallObstacleRuntime GetOrCreateObstacleVisual(ObstacleStatePayload payload)
        {
            if (_obstacleVisuals.TryGetValue(payload.obstacleId, out var runtime) && runtime != null)
            {
                return runtime;
            }

            runtime = new WallObstacleRuntime($"WallObstacle_{payload.obstacleId}", payload.obstacleId, _obstacleVisualRoot, payload.size, isPreview: false);
            runtime.SetHp(payload.currentHp, payload.maxHp);
            runtime.SetColliderEnabled(true);
            _obstacleVisuals[payload.obstacleId] = runtime;
            return runtime;
        }

        private void BroadcastObstacleState(ObstacleStatePayload payload)
        {
            if (_selectedRole != NetworkRole.Host || payload == null)
            {
                return;
            }

            _networkCoordinator?.NotifyHostObstacleState(
                payload.obstacleId,
                payload.position,
                payload.rotation,
                payload.size,
                payload.currentHp,
                payload.maxHp,
                payload.active);
        }

        private void BroadcastAllObstacleStates()
        {
            if (_selectedRole != NetworkRole.Host)
            {
                return;
            }

            foreach (var pair in _obstacleStates)
            {
                BroadcastObstacleState(pair.Value);
            }
        }

        private void ResetObstaclesForNewMatch()
        {
            if (_selectedRole == NetworkRole.Host)
            {
                var ids = new List<int>();
                ids.AddRange(_obstacleStates.Keys);
                for (var index = 0; index < ids.Count; index++)
                {
                    var obstacleId = ids[index];
                    _networkCoordinator?.NotifyHostObstacleState(obstacleId, Vector3.zero, Quaternion.identity, GetWallSize(), 0f, GetWallMaxHp(), false);
                }
            }

            ClearObstacleVisuals();
            _obstacleStates.Clear();
            CancelSpectatorWallPlacement();
        }

        private void ClearObstacleVisuals()
        {
            foreach (var pair in _obstacleVisuals)
            {
                pair.Value?.Dispose();
            }

            _obstacleVisuals.Clear();
            CancelSpectatorWallPlacement();
        }

        private int GetActiveObstacleCount()
        {
            var count = 0;
            foreach (var pair in _obstacleStates)
            {
                if (pair.Value != null && pair.Value.active)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetArenaWorldBasis(out Vector3 center, out Vector3 forward, out Vector3 right, out float baseYaw)
        {
            center = Vector3.zero;
            forward = Vector3.forward;
            right = Vector3.right;
            baseYaw = 0f;

            if (!TryGetArenaHeadPositionsForAuthority(out var hostHead, out var clientHead))
            {
                return false;
            }

            center = (hostHead + clientHead) * 0.5f;
            var flatDelta = Vector3.ProjectOnPlane(clientHead - hostHead, Vector3.up);
            if (flatDelta.sqrMagnitude < 0.0001f)
            {
                flatDelta = Vector3.forward;
            }

            forward = flatDelta.normalized;
            right = Vector3.Cross(Vector3.up, forward).normalized;
            baseYaw = Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
            return true;
        }

        private bool TryGetArenaHeadPositionsForAuthority(out Vector3 hostHead, out Vector3 clientHead)
        {
            hostHead = Vector3.zero;
            clientHead = Vector3.zero;
            if (_selectedRole != NetworkRole.Host)
            {
                return false;
            }

            if (Camera.main == null)
            {
                return false;
            }

            hostHead = Camera.main.transform.position;
            if (!TryGetAlignedRemoteHeadPosition(out clientHead))
            {
                return false;
            }

            return true;
        }

        private bool TryGetArenaDisplayBasis(out Vector3 center, out Vector3 forward, out Vector3 right, out float baseYaw)
        {
            center = Vector3.zero;
            forward = Vector3.forward;
            right = Vector3.right;
            baseYaw = 0f;

            if (_selectedRole != NetworkRole.Spectator ||
                _spectatorHostProxy == null ||
                _spectatorClientProxy == null ||
                _spectatorHostProxy.HeadTransform == null ||
                _spectatorClientProxy.HeadTransform == null)
            {
                return false;
            }

            var hostHead = _spectatorHostProxy.HeadTransform.position;
            var clientHead = _spectatorClientProxy.HeadTransform.position;
            center = (hostHead + clientHead) * 0.5f;
            var flatDelta = Vector3.ProjectOnPlane(clientHead - hostHead, Vector3.up);
            if (flatDelta.sqrMagnitude < 0.0001f)
            {
                flatDelta = Vector3.forward;
            }

            forward = flatDelta.normalized;
            right = Vector3.Cross(Vector3.up, forward).normalized;
            baseYaw = Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
            return true;
        }

        private bool TryGetArenaHeadPositionsRaw(out Vector3 hostHead, out Vector3 clientHead)
        {
            hostHead = Vector3.zero;
            clientHead = Vector3.zero;
            if (_networkCoordinator == null)
            {
                return false;
            }

            if (_selectedRole == NetworkRole.Host)
            {
                hostHead = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                if (!_networkCoordinator.TryGetRemotePose(NetworkRole.Client, out var clientPose) || clientPose == null)
                {
                    return false;
                }

                clientHead = clientPose.head.position;
                return true;
            }

            if (_selectedRole == NetworkRole.Client)
            {
                clientHead = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                if (!_networkCoordinator.TryGetRemotePose(NetworkRole.Host, out var hostPose) || hostPose == null)
                {
                    return false;
                }

                hostHead = hostPose.head.position;
                return true;
            }

            if (_selectedRole == NetworkRole.Spectator)
            {
                if (!_networkCoordinator.TryGetRemotePose(NetworkRole.Host, out var hostPose) ||
                    !_networkCoordinator.TryGetRemotePose(NetworkRole.Client, out var clientPose) ||
                    hostPose == null ||
                    clientPose == null)
                {
                    return false;
                }

                hostHead = hostPose.head.position;
                clientHead = clientPose.head.position;
                return true;
            }

            return false;
        }

        private int GetMaxHp() => combatBalanceConfig != null ? combatBalanceConfig.hp : 100;
        private int GetDamage() => combatBalanceConfig != null ? combatBalanceConfig.damage : 10;
        private float GetProjectileSpeed() => combatBalanceConfig != null ? combatBalanceConfig.projectileSpeed : 5f;
        private float GetProjectileRadius() => combatBalanceConfig != null ? combatBalanceConfig.projectileRadius : 0.033f;
        private float GetShootCooldown() => combatBalanceConfig != null ? combatBalanceConfig.shootCooldown : 1f;
        private float GetShieldDuration() => combatBalanceConfig != null ? combatBalanceConfig.shieldDuration : 1.5f;
        private float GetShieldCooldown() => combatBalanceConfig != null ? combatBalanceConfig.shieldCooldown : 3f;
        private int GetSpectatorHealAmount() => spectatorSupportConfig != null ? spectatorSupportConfig.healAmount : 10;
        private float GetSpectatorVoteCooldown() => spectatorSupportConfig != null ? spectatorSupportConfig.voteCooldown : 3f;
        private string GetSpectatorBarrageWordA() => spectatorSupportConfig != null ? spectatorSupportConfig.barrageWordA : "COOL";
        private string GetSpectatorBarrageWordB() => spectatorSupportConfig != null ? spectatorSupportConfig.barrageWordB : "GOOD GAME";
        private string GetSpectatorBarrageWordC() => spectatorSupportConfig != null ? spectatorSupportConfig.barrageWordC : "NICE SHOT";
        private float GetSpectatorBarrageDuration() => spectatorSupportConfig != null ? spectatorSupportConfig.barrageDuration : 2.4f;
        private float GetSpectatorBarrageSpeed() => spectatorSupportConfig != null ? spectatorSupportConfig.barrageSpeed : 0.42f;
        private AudioClip GetSpectatorCheerClip()
        {
            if (spectatorSupportConfig != null && spectatorSupportConfig.cheerClip != null)
            {
                return spectatorSupportConfig.cheerClip;
            }

            return Resources.Load<AudioClip>("Audio/yay");
        }

        private AudioClip GetSpectatorApplauseClip()
        {
            if (spectatorSupportConfig != null && spectatorSupportConfig.applauseClip != null)
            {
                return spectatorSupportConfig.applauseClip;
            }

            return Resources.Load<AudioClip>("Audio/cheer");
        }
        private float GetSpectatorAudioVolume() => spectatorSupportConfig != null ? spectatorSupportConfig.audioVolume : 0.9f;
        private int GetWallMaxHp() => spectatorSupportConfig != null ? spectatorSupportConfig.wallMaxHp : 100;
        private float GetWallDecayPerSecond() => spectatorSupportConfig != null ? spectatorSupportConfig.wallDecayPerSecond : 5f;
        private int GetWallShotDamage() => spectatorSupportConfig != null ? spectatorSupportConfig.wallShotDamage : 10;
        private float GetWallPlacementDistance() => spectatorSupportConfig != null ? spectatorSupportConfig.wallPlacementDistance : 1.4f;
        private float GetWallSpawnCooldown() => spectatorSupportConfig != null ? spectatorSupportConfig.wallSpawnCooldown : 2f;
        private int GetWallMaxActiveCount() => spectatorSupportConfig != null ? spectatorSupportConfig.wallMaxActiveCount : 2;
        private Vector3 GetWallSize() => spectatorSupportConfig != null ? spectatorSupportConfig.wallSize : new Vector3(1.6f, 1.35f, 0.12f);

        private static string LoadHostIpPreference(string fallback)
        {
            var value = PlayerPrefs.GetString(HostIpPlayerPrefsKey, string.Empty);
            return SanitizeHostIp(string.IsNullOrWhiteSpace(value) ? fallback : value);
        }

        private void SaveHostIpPreference()
        {
            PlayerPrefs.SetString(HostIpPlayerPrefsKey, SanitizeHostIp(hostIpForClient));
            PlayerPrefs.Save();
        }

        private static string SanitizeHostIp(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

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

        private void UpdateSpectatorVoteUi()
        {
            if (_spectatorControlView == null)
            {
                return;
            }

            if (_selectedRole != NetworkRole.Spectator || _stateMachine == null || _stateMachine.CurrentId != AppStateId.Playing)
            {
                _spectatorControlView.SetVisible(false);
                return;
            }

            _spectatorControlView.SetVisible(true);
            _spectatorControlView.SetBarrageLabels(GetSpectatorBarrageWordA(), GetSpectatorBarrageWordB(), GetSpectatorBarrageWordC());
            var cooldownRemaining = Mathf.Max(0f, _localSpectatorVoteCooldownUntil - Time.time);
            var cheerReady = GetSpectatorCheerClip() != null ? "Ready" : "Missing";
            var applauseReady = GetSpectatorApplauseClip() != null ? "Ready" : "Missing";
            var wallInfo = _spectatorWallPlacementActive
                ? "Wall: placement active"
                : $"Wall: {GetActiveObstacleCount()}/{GetWallMaxActiveCount()} active";
            _spectatorControlView.SetStatus($"Vote Heal: +{GetSpectatorHealAmount()} HP\nCooldown: {cooldownRemaining:F1}s\nBarrage: local-only\nAudio: Cheer {cheerReady} / Applause {applauseReady}\n{wallInfo}");
            var interactable = cooldownRemaining <= 0.001f && !_spectatorWallPlacementActive;
            var canPlaceWall = !_spectatorWallPlacementActive;
            _spectatorControlView.SetButtonsInteractable(interactable, interactable, !_spectatorWallPlacementActive, !_spectatorWallPlacementActive, canPlaceWall);
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

        private static Transform EnsureNamedRootExists(string rootName)
        {
            var existing = GameObject.Find(rootName);
            if (existing != null)
            {
                return existing.transform;
            }

            var root = new GameObject(rootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root.transform;
        }

        private M3RemotePlayerProxy CreateSpectatorProxy(string proxyName, Transform alignmentRoot)
        {
            var existing = transform.Find(proxyName);
            GameObject proxyGo;
            if (existing != null)
            {
                proxyGo = existing.gameObject;
            }
            else
            {
                proxyGo = new GameObject(proxyName);
                proxyGo.transform.SetParent(transform, false);
            }

            var proxy = proxyGo.GetComponent<M3RemotePlayerProxy>();
            if (proxy == null)
            {
                proxy = proxyGo.AddComponent<M3RemotePlayerProxy>();
            }

            proxy.BindAlignmentRoot(alignmentRoot);
            return proxy;
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



