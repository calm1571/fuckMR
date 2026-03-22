using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Networking
{
    public sealed class M3NetworkCoordinator
    {
        private readonly UdpLanTransport _transport = new UdpLanTransport();
        private readonly string _localPlayerId = Guid.NewGuid().ToString("N");
        private readonly Dictionary<NetworkRole, PosePayload> _remotePosesByRole = new Dictionary<NetworkRole, PosePayload>();

        private readonly int _port;
        private readonly string _defaultHostIp;
        private readonly float _poseSendInterval;

        private float _nextPoseTime;
        private PosePayload _latestRemotePose;
        private bool _hasRemotePose;
        private bool _remoteCalibrationRequested;
        private bool _remoteWorldRootSyncRequested;
        private bool _remoteShootRequested;
        private bool _remoteShieldRequested;
        private bool _remoteHpUpdateRequested;
        private bool _remoteMatchResultRequested;
        private bool _remoteSharedAnchorRequested;
        private bool _remoteStartPlayingRequested;
        private bool _remoteCalibrationReadyRequested;
        private bool _remoteAlignmentRequested;
        private bool _remoteRematchReadyRequested;
        private bool _remoteSpectatorVoteRequested;
        private bool _remoteObstacleSpawnRequestRequested;
        private bool _remoteObstacleStateRequested;
        private NetworkRole _pendingShootSenderRole;
        private NetworkRole _pendingShieldSenderRole;
        private WorldRootSyncPayload _pendingWorldRootSync;
        private ShootPayload _pendingShoot;
        private ShieldPayload _pendingShield;
        private HpUpdatePayload _pendingHpUpdate;
        private MatchResultPayload _pendingMatchResult;
        private SharedAnchorPayload _pendingSharedAnchor;
        private CalibrationReadyPayload _pendingCalibrationReady;
        private RemoteAlignmentPayload _pendingRemoteAlignment;
        private RematchReadyPayload _pendingRematchReady;
        private SpectatorVotePayload _pendingSpectatorVote;
        private ObstacleSpawnRequestPayload _pendingObstacleSpawnRequest;
        private ObstacleStatePayload _pendingObstacleState;

        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;

        public event Action RemoteCalibrationRequested;
        public event Action<WorldRootSyncPayload> WorldRootSyncReceived;
        public event Action<ShootPayload> RemoteShootReceived;
        public event Action<NetworkRole, ShootPayload> RoleShootReceived;
        public event Action<ShieldPayload> RemoteShieldReceived;
        public event Action<NetworkRole, ShieldPayload> RoleShieldReceived;
        public event Action<HpUpdatePayload> HpUpdateReceived;
        public event Action<MatchResultPayload> MatchResultReceived;
        public event Action<SharedAnchorPayload> SharedAnchorReceived;
        public event Action StartPlayingRequested;
        public event Action<CalibrationReadyPayload> RemoteCalibrationReadyReceived;
        public event Action<RemoteAlignmentPayload> RemoteAlignmentReceived;
        public event Action<RematchReadyPayload> RemoteRematchReadyReceived;
        public event Action<SpectatorVotePayload> SpectatorVoteReceived;
        public event Action<ObstacleSpawnRequestPayload> ObstacleSpawnRequestReceived;
        public event Action<ObstacleStatePayload> ObstacleStateReceived;

        public NetworkRole Role { get; private set; } = NetworkRole.None;
        public bool IsConnected => _transport.IsConnected;
        public bool HasRemotePose => _hasRemotePose;
        public PosePayload LatestRemotePose => _latestRemotePose;
        public bool HasClientPeer => _transport.HasPeer(NetworkRole.Client);
        public int SpectatorCount => _transport.GetPeerCount(NetworkRole.Spectator);

        public M3NetworkCoordinator(int port, string defaultHostIp, float poseSendRate)
        {
            _port = port;
            _defaultHostIp = defaultHostIp;
            _poseSendInterval = 1f / Mathf.Max(5f, poseSendRate);
            _transport.Connected += OnTransportConnected;
            _transport.MessageReceived += OnMessageReceived;
        }

        public void BindLocalRig(Transform head, Transform leftHand, Transform rightHand)
        {
            _head = head;
            _leftHand = leftHand;
            _rightHand = rightHand;
        }

        public void StartHost()
        {
            Role = NetworkRole.Host;
            _transport.StartHost(_port, _localPlayerId);
            ResetRuntimeState();
        }

        public void StartClient(string hostIp = null)
        {
            Role = NetworkRole.Client;
            _transport.StartClient(_port, _localPlayerId, string.IsNullOrEmpty(hostIp) ? _defaultHostIp : hostIp);
            ResetRuntimeState();
        }

        public void StartSpectator(string hostIp = null)
        {
            Role = NetworkRole.Spectator;
            _transport.StartSpectator(_port, _localPlayerId, string.IsNullOrEmpty(hostIp) ? _defaultHostIp : hostIp);
            ResetRuntimeState();
        }

        public void Stop()
        {
            Role = NetworkRole.None;
            _transport.Stop();
            ResetRuntimeState();
        }

        public void Tick(float unscaledTime)
        {
            _transport.Tick(unscaledTime);

            if (_remoteCalibrationRequested)
            {
                _remoteCalibrationRequested = false;
                RemoteCalibrationRequested?.Invoke();
            }

            if (_remoteWorldRootSyncRequested)
            {
                _remoteWorldRootSyncRequested = false;
                if (_pendingWorldRootSync != null)
                {
                    WorldRootSyncReceived?.Invoke(_pendingWorldRootSync);
                }
            }

            if (_remoteShootRequested)
            {
                _remoteShootRequested = false;
                if (_pendingShoot != null)
                {
                    RoleShootReceived?.Invoke(_pendingShootSenderRole, _pendingShoot);
                    if (IsCounterpartPlayerRole(_pendingShootSenderRole))
                    {
                        RemoteShootReceived?.Invoke(_pendingShoot);
                    }
                }
            }

            if (_remoteShieldRequested)
            {
                _remoteShieldRequested = false;
                if (_pendingShield != null)
                {
                    RoleShieldReceived?.Invoke(_pendingShieldSenderRole, _pendingShield);
                    if (IsCounterpartPlayerRole(_pendingShieldSenderRole))
                    {
                        RemoteShieldReceived?.Invoke(_pendingShield);
                    }
                }
            }

            if (_remoteHpUpdateRequested)
            {
                _remoteHpUpdateRequested = false;
                if (_pendingHpUpdate != null)
                {
                    HpUpdateReceived?.Invoke(_pendingHpUpdate);
                }
            }

            if (_remoteMatchResultRequested)
            {
                _remoteMatchResultRequested = false;
                if (_pendingMatchResult != null)
                {
                    MatchResultReceived?.Invoke(_pendingMatchResult);
                }
            }

            if (_remoteSharedAnchorRequested)
            {
                _remoteSharedAnchorRequested = false;
                if (_pendingSharedAnchor != null)
                {
                    SharedAnchorReceived?.Invoke(_pendingSharedAnchor);
                }
            }

            if (_remoteStartPlayingRequested)
            {
                _remoteStartPlayingRequested = false;
                StartPlayingRequested?.Invoke();
            }

            if (_remoteCalibrationReadyRequested)
            {
                _remoteCalibrationReadyRequested = false;
                if (_pendingCalibrationReady != null)
                {
                    RemoteCalibrationReadyReceived?.Invoke(_pendingCalibrationReady);
                }
            }

            if (_remoteAlignmentRequested)
            {
                _remoteAlignmentRequested = false;
                if (_pendingRemoteAlignment != null)
                {
                    RemoteAlignmentReceived?.Invoke(_pendingRemoteAlignment);
                }
            }

            if (_remoteRematchReadyRequested)
            {
                _remoteRematchReadyRequested = false;
                if (_pendingRematchReady != null)
                {
                    RemoteRematchReadyReceived?.Invoke(_pendingRematchReady);
                }
            }

            if (_remoteSpectatorVoteRequested)
            {
                _remoteSpectatorVoteRequested = false;
                if (_pendingSpectatorVote != null)
                {
                    SpectatorVoteReceived?.Invoke(_pendingSpectatorVote);
                }
            }

            if (_remoteObstacleSpawnRequestRequested)
            {
                _remoteObstacleSpawnRequestRequested = false;
                if (_pendingObstacleSpawnRequest != null)
                {
                    ObstacleSpawnRequestReceived?.Invoke(_pendingObstacleSpawnRequest);
                }
            }

            if (_remoteObstacleStateRequested)
            {
                _remoteObstacleStateRequested = false;
                if (_pendingObstacleState != null)
                {
                    ObstacleStateReceived?.Invoke(_pendingObstacleState);
                }
            }

            if (!IsConnected || _head == null || Role == NetworkRole.Spectator)
            {
                return;
            }

            if (unscaledTime < _nextPoseTime)
            {
                return;
            }

            _nextPoseTime = unscaledTime + _poseSendInterval;
            _transport.SendPose(CaptureLocalPose());
        }

        public bool TryGetRemotePose(NetworkRole role, out PosePayload payload)
        {
            return _remotePosesByRole.TryGetValue(role, out payload) && payload != null;
        }

        public bool HasRemotePoseForRole(NetworkRole role)
        {
            return _remotePosesByRole.ContainsKey(role) && _remotePosesByRole[role] != null;
        }

        public void NotifyHostStartCalibration()
        {
            if (Role == NetworkRole.Host)
            {
                _transport.SendStartCalibration();
            }
        }

        public void NotifyHostWorldRootSync(Vector3 position, Quaternion rotation)
        {
            if (Role != NetworkRole.Host)
            {
                return;
            }

            var payload = new WorldRootSyncPayload
            {
                position = position,
                rotation = rotation
            };
            _transport.SendWorldRootSync(payload);
        }

        public void NotifyShoot(Vector3 spawnPosition, Vector3 direction, float speed, float maxDistance, float lifetime)
        {
            if (Role == NetworkRole.None || !_transport.IsConnected || Role == NetworkRole.Spectator)
            {
                return;
            }

            var payload = new ShootPayload
            {
                spawnPosition = spawnPosition,
                direction = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized,
                speed = speed,
                maxDistance = maxDistance,
                lifetime = lifetime
            };
            _transport.SendShoot(payload);
        }

        public void NotifyShield(bool active, float duration)
        {
            if (Role == NetworkRole.None || !_transport.IsConnected || Role == NetworkRole.Spectator)
            {
                return;
            }

            var payload = new ShieldPayload
            {
                active = active,
                duration = duration
            };
            _transport.SendShield(payload);
        }

        public void NotifyHostHpUpdate(int hostHp, int clientHp)
        {
            if (Role != NetworkRole.Host || !_transport.IsConnected)
            {
                return;
            }

            var payload = new HpUpdatePayload
            {
                hostHp = hostHp,
                clientHp = clientHp
            };
            _transport.SendHpUpdate(payload);
        }

        public void NotifyHostMatchResult(string winnerRole)
        {
            if (Role != NetworkRole.Host || !_transport.IsConnected)
            {
                return;
            }

            var payload = new MatchResultPayload
            {
                winnerRole = winnerRole
            };
            _transport.SendMatchResult(payload);
        }

        public void NotifyHostSharedAnchor(string uuid)
        {
            if (Role != NetworkRole.Host || !_transport.IsConnected || string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var payload = new SharedAnchorPayload
            {
                uuid = uuid
            };
            _transport.SendSharedAnchor(payload);
        }

        public void NotifyHostStartPlaying()
        {
            if (Role != NetworkRole.Host || !_transport.IsConnected)
            {
                return;
            }

            _transport.SendStartPlaying();
        }

        public void NotifyCalibrationReady(bool ready, bool hasPose, bool isLocked, float stability01)
        {
            if (Role == NetworkRole.None || !_transport.IsConnected || Role == NetworkRole.Spectator)
            {
                return;
            }

            var payload = new CalibrationReadyPayload
            {
                ready = ready,
                hasPose = hasPose,
                isLocked = isLocked,
                stability01 = Mathf.Clamp01(stability01)
            };

            _transport.SendCalibrationReady(payload);
        }

        public void NotifyRemoteAlignment(Vector3 position, Quaternion rotation, bool confirmed, string stage)
        {
            if (Role == NetworkRole.None || !_transport.IsConnected || Role == NetworkRole.Spectator)
            {
                return;
            }

            var payload = new RemoteAlignmentPayload
            {
                position = position,
                rotation = rotation,
                senderRole = Role.ToString(),
                stage = stage,
                confirmed = confirmed
            };

            _transport.SendRemoteAlignment(payload);
        }

        public void NotifyRematchReady(bool ready)
        {
            if (Role == NetworkRole.None || !_transport.IsConnected || Role == NetworkRole.Spectator)
            {
                return;
            }

            var payload = new RematchReadyPayload
            {
                ready = ready
            };

            _transport.SendRematchReady(payload);
        }

        public void NotifySpectatorVote(string targetRole)
        {
            if (Role != NetworkRole.Spectator || !_transport.IsConnected || string.IsNullOrEmpty(targetRole))
            {
                return;
            }

            var payload = new SpectatorVotePayload
            {
                targetRole = targetRole
            };

            _transport.SendSpectatorVote(payload);
        }

        public void NotifyObstacleSpawnRequest(string anchorType, Vector3 localOffset, float yawOffset)
        {
            if (Role != NetworkRole.Spectator || !_transport.IsConnected || string.IsNullOrEmpty(anchorType))
            {
                return;
            }

            var payload = new ObstacleSpawnRequestPayload
            {
                anchorType = anchorType,
                localOffset = localOffset,
                yawOffset = yawOffset
            };

            _transport.SendObstacleSpawnRequest(payload);
        }

        public void NotifyHostObstacleState(int obstacleId, Vector3 position, Quaternion rotation, Vector3 size, float currentHp, float maxHp, bool active)
        {
            if (Role != NetworkRole.Host || !_transport.IsConnected)
            {
                return;
            }

            var payload = new ObstacleStatePayload
            {
                obstacleId = obstacleId,
                position = position,
                rotation = rotation,
                size = size,
                currentHp = currentHp,
                maxHp = maxHp,
                active = active
            };

            _transport.SendObstacleState(payload);
        }

        public string BuildLobbyStatus()
        {
            if (Role == NetworkRole.Host)
            {
                var clientState = HasClientPeer ? "Client Connected" : "Waiting for client...";
                return $"{clientState}\nSpectators: {SpectatorCount}";
            }

            if (Role == NetworkRole.Client)
            {
                return IsConnected ? "Connected to host" : $"Connecting to {_defaultHostIp}:{_port} ...";
            }

            if (Role == NetworkRole.Spectator)
            {
                return IsConnected ? "Connected to host as spectator" : $"Connecting to {_defaultHostIp}:{_port} ...";
            }

            return "Network idle";
        }

        private void ResetRuntimeState()
        {
            _hasRemotePose = false;
            _latestRemotePose = null;
            _remotePosesByRole.Clear();
            _remoteCalibrationRequested = false;
            _remoteWorldRootSyncRequested = false;
            _pendingWorldRootSync = null;
            _remoteShootRequested = false;
            _pendingShoot = null;
            _pendingShootSenderRole = NetworkRole.None;
            _remoteShieldRequested = false;
            _pendingShield = null;
            _pendingShieldSenderRole = NetworkRole.None;
            _remoteHpUpdateRequested = false;
            _pendingHpUpdate = null;
            _remoteMatchResultRequested = false;
            _pendingMatchResult = null;
            _remoteSharedAnchorRequested = false;
            _pendingSharedAnchor = null;
            _remoteStartPlayingRequested = false;
            _remoteCalibrationReadyRequested = false;
            _pendingCalibrationReady = null;
            _remoteAlignmentRequested = false;
            _pendingRemoteAlignment = null;
            _remoteRematchReadyRequested = false;
            _pendingRematchReady = null;
            _remoteSpectatorVoteRequested = false;
            _pendingSpectatorVote = null;
            _remoteObstacleSpawnRequestRequested = false;
            _pendingObstacleSpawnRequest = null;
            _remoteObstacleStateRequested = false;
            _pendingObstacleState = null;
            _nextPoseTime = 0f;
        }

        private PosePayload CaptureLocalPose()
        {
            return new PosePayload
            {
                head = BuildPose(_head),
                leftHand = BuildPose(_leftHand),
                rightHand = BuildPose(_rightHand)
            };
        }

        private static PoseData BuildPose(Transform source)
        {
            return source == null
                ? new PoseData { position = Vector3.zero, rotation = Quaternion.identity }
                : new PoseData { position = source.position, rotation = source.rotation };
        }

        private void OnTransportConnected()
        {
            Debug.Log($"M3 Net: Connected as {Role}");
        }

        private void OnMessageReceived(LanMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.type))
            {
                return;
            }

            if (message.playerId == _localPlayerId)
            {
                return;
            }

            var senderRole = ParseRole(message.senderRole);
            if (message.type == LanMessageTypes.Pose && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    var pose = JsonUtility.FromJson<PosePayload>(message.payload);
                    if (pose != null && senderRole != NetworkRole.None)
                    {
                        _remotePosesByRole[senderRole] = pose;
                        if (IsCounterpartPlayerRole(senderRole))
                        {
                            _latestRemotePose = pose;
                            _hasRemotePose = true;
                        }
                    }
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.StartCalibration && Role == NetworkRole.Client)
            {
                _remoteCalibrationRequested = true;
            }
            else if (message.type == LanMessageTypes.WorldRootSync &&
                     (Role == NetworkRole.Client || Role == NetworkRole.Spectator) &&
                     !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingWorldRootSync = JsonUtility.FromJson<WorldRootSyncPayload>(message.payload);
                    _remoteWorldRootSyncRequested = _pendingWorldRootSync != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.Shoot && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingShoot = JsonUtility.FromJson<ShootPayload>(message.payload);
                    _pendingShootSenderRole = senderRole;
                    _remoteShootRequested = _pendingShoot != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.Shield && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingShield = JsonUtility.FromJson<ShieldPayload>(message.payload);
                    _pendingShieldSenderRole = senderRole;
                    _remoteShieldRequested = _pendingShield != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.HpUpdate && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingHpUpdate = JsonUtility.FromJson<HpUpdatePayload>(message.payload);
                    _remoteHpUpdateRequested = _pendingHpUpdate != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.MatchResult && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingMatchResult = JsonUtility.FromJson<MatchResultPayload>(message.payload);
                    _remoteMatchResultRequested = _pendingMatchResult != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.SharedAnchor && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingSharedAnchor = JsonUtility.FromJson<SharedAnchorPayload>(message.payload);
                    _remoteSharedAnchorRequested = _pendingSharedAnchor != null && !string.IsNullOrEmpty(_pendingSharedAnchor.uuid);
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.StartPlaying && Role != NetworkRole.Host)
            {
                _remoteStartPlayingRequested = true;
            }
            else if (message.type == LanMessageTypes.CalibrationReady && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingCalibrationReady = JsonUtility.FromJson<CalibrationReadyPayload>(message.payload);
                    _remoteCalibrationReadyRequested = _pendingCalibrationReady != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.RemoteAlignment && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingRemoteAlignment = JsonUtility.FromJson<RemoteAlignmentPayload>(message.payload);
                    _remoteAlignmentRequested = _pendingRemoteAlignment != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.RematchReady && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingRematchReady = JsonUtility.FromJson<RematchReadyPayload>(message.payload);
                    _remoteRematchReadyRequested = _pendingRematchReady != null;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.SpectatorVote &&
                     Role == NetworkRole.Host &&
                     senderRole == NetworkRole.Spectator &&
                     !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingSpectatorVote = JsonUtility.FromJson<SpectatorVotePayload>(message.payload);
                    _remoteSpectatorVoteRequested = _pendingSpectatorVote != null && !string.IsNullOrEmpty(_pendingSpectatorVote.targetRole);
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.ObstacleSpawnRequest &&
                     Role == NetworkRole.Host &&
                     senderRole == NetworkRole.Spectator &&
                     !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingObstacleSpawnRequest = JsonUtility.FromJson<ObstacleSpawnRequestPayload>(message.payload);
                    _remoteObstacleSpawnRequestRequested = _pendingObstacleSpawnRequest != null && !string.IsNullOrEmpty(_pendingObstacleSpawnRequest.anchorType);
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.ObstacleState &&
                     Role != NetworkRole.Host &&
                     !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingObstacleState = JsonUtility.FromJson<ObstacleStatePayload>(message.payload);
                    _remoteObstacleStateRequested = _pendingObstacleState != null;
                }
                catch
                {
                }
            }
        }

        private bool IsCounterpartPlayerRole(NetworkRole senderRole)
        {
            if (Role == NetworkRole.Host)
            {
                return senderRole == NetworkRole.Client;
            }

            if (Role == NetworkRole.Client)
            {
                return senderRole == NetworkRole.Host;
            }

            return senderRole == NetworkRole.Host || senderRole == NetworkRole.Client;
        }

        private static NetworkRole ParseRole(string value)
        {
            return Enum.TryParse(value, true, out NetworkRole parsed) ? parsed : NetworkRole.None;
        }
    }
}
