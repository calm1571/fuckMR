using System;
using UnityEngine;

namespace Project.Networking
{
    public sealed class M3NetworkCoordinator
    {
        private readonly UdpLanTransport _transport = new UdpLanTransport();
        private readonly string _localPlayerId = Guid.NewGuid().ToString("N");

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
        private WorldRootSyncPayload _pendingWorldRootSync;
        private ShootPayload _pendingShoot;
        private ShieldPayload _pendingShield;
        private HpUpdatePayload _pendingHpUpdate;
        private MatchResultPayload _pendingMatchResult;

        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;

        public event Action RemoteCalibrationRequested;
        public event Action<WorldRootSyncPayload> WorldRootSyncReceived;
        public event Action<ShootPayload> RemoteShootReceived;
        public event Action<ShieldPayload> RemoteShieldReceived;
        public event Action<HpUpdatePayload> HpUpdateReceived;
        public event Action<MatchResultPayload> MatchResultReceived;

        public NetworkRole Role { get; private set; } = NetworkRole.None;
        public bool IsConnected => _transport.IsConnected;
        public bool HasRemotePose => _hasRemotePose;
        public PosePayload LatestRemotePose => _latestRemotePose;

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
            _hasRemotePose = false;
            _remoteCalibrationRequested = false;
            _remoteWorldRootSyncRequested = false;
            _pendingWorldRootSync = null;
            _remoteShootRequested = false;
            _pendingShoot = null;
            _remoteShieldRequested = false;
            _pendingShield = null;
            _remoteHpUpdateRequested = false;
            _pendingHpUpdate = null;
            _remoteMatchResultRequested = false;
            _pendingMatchResult = null;
        }

        public void StartClient(string hostIp = null)
        {
            Role = NetworkRole.Client;
            _transport.StartClient(_port, _localPlayerId, string.IsNullOrEmpty(hostIp) ? _defaultHostIp : hostIp);
            _hasRemotePose = false;
            _remoteCalibrationRequested = false;
            _remoteWorldRootSyncRequested = false;
            _pendingWorldRootSync = null;
            _remoteShootRequested = false;
            _pendingShoot = null;
            _remoteShieldRequested = false;
            _pendingShield = null;
            _remoteHpUpdateRequested = false;
            _pendingHpUpdate = null;
            _remoteMatchResultRequested = false;
            _pendingMatchResult = null;
        }

        public void Stop()
        {
            Role = NetworkRole.None;
            _transport.Stop();
            _hasRemotePose = false;
            _remoteCalibrationRequested = false;
            _remoteWorldRootSyncRequested = false;
            _pendingWorldRootSync = null;
            _remoteShootRequested = false;
            _pendingShoot = null;
            _remoteShieldRequested = false;
            _pendingShield = null;
            _remoteHpUpdateRequested = false;
            _pendingHpUpdate = null;
            _remoteMatchResultRequested = false;
            _pendingMatchResult = null;
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
                    RemoteShootReceived?.Invoke(_pendingShoot);
                }
            }

            if (_remoteShieldRequested)
            {
                _remoteShieldRequested = false;
                if (_pendingShield != null)
                {
                    RemoteShieldReceived?.Invoke(_pendingShield);
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

            if (!IsConnected || _head == null)
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
            if (Role == NetworkRole.None || !_transport.IsConnected)
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
            if (Role == NetworkRole.None || !_transport.IsConnected)
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

        public string BuildLobbyStatus()
        {
            if (Role == NetworkRole.Host)
            {
                return IsConnected ? "Client Connected" : "Waiting for client...";
            }

            if (Role == NetworkRole.Client)
            {
                return IsConnected ? "Connected to host" : $"Connecting to {_defaultHostIp}:{_port} ...";
            }

            return "Network idle";
        }

        private PosePayload CaptureLocalPose()
        {
            var payload = new PosePayload
            {
                head = BuildPose(_head),
                leftHand = BuildPose(_leftHand),
                rightHand = BuildPose(_rightHand)
            };
            return payload;
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

            if (message.type == LanMessageTypes.Pose && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _latestRemotePose = JsonUtility.FromJson<PosePayload>(message.payload);
                    _hasRemotePose = true;
                }
                catch
                {
                }
            }
            else if (message.type == LanMessageTypes.StartCalibration && Role == NetworkRole.Client)
            {
                Debug.Log("M4 Net: Client received START_CALIBRATION");
                _remoteCalibrationRequested = true;
            }
            else if (message.type == LanMessageTypes.WorldRootSync && Role == NetworkRole.Client && !string.IsNullOrEmpty(message.payload))
            {
                try
                {
                    _pendingWorldRootSync = JsonUtility.FromJson<WorldRootSyncPayload>(message.payload);
                    _remoteWorldRootSyncRequested = _pendingWorldRootSync != null;
                    if (_remoteWorldRootSyncRequested)
                    {
                        Debug.Log("M4 Net: Client received WORLD_ROOT_SYNC");
                    }
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
        }
    }
}
