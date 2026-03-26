using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Project.Networking
{
        /// <summary>
    /// 基于 UDP 的局域网传输层，负责握手、收包、发包和 Host 中继。
    /// </summary>
    public sealed class UdpLanTransport
    {
                /// <summary>
        /// Host 端记录的已连接远端节点信息。
        /// </summary>
        private sealed class HostPeerInfo
        {
            public IPEndPoint endpoint;
            public NetworkRole role;
        }

        private readonly Queue<LanMessage> _inbox = new Queue<LanMessage>();
        private readonly object _inboxLock = new object();
        private readonly Dictionary<string, HostPeerInfo> _hostPeers = new Dictionary<string, HostPeerInfo>();

        private UdpClient _udpClient;
        private IPEndPoint _serverEndpoint;
        private string _localPlayerId;
        private NetworkRole _role = NetworkRole.None;
        private float _lastHelloTime;
        private bool _connected;
        private string _lastDiagnostic = "Idle";

        public event Action Connected;
        public event Action<LanMessage> MessageReceived;

        public bool IsConnected => _connected;
        public NetworkRole Role => _role;
        public int TotalPeerCount => _role == NetworkRole.Host ? _hostPeers.Count : (_connected ? 1 : 0);
        public string LastDiagnostic => _lastDiagnostic;

        public void StartHost(int port, string localPlayerId)
        {
            Stop();
            _localPlayerId = localPlayerId;
            _role = NetworkRole.Host;
            _connected = false;

            _udpClient = new UdpClient(port);
            _udpClient.EnableBroadcast = true;
            BeginReceive();
            SetDiagnostic($"Host listening on UDP {port}");
            Debug.Log($"M3 Net: Host started on UDP {port}");
        }

        public void StartClient(int port, string localPlayerId, string hostIp)
        {
            StartPeer(port, localPlayerId, hostIp, NetworkRole.Client);
        }

        public void StartSpectator(int port, string localPlayerId, string hostIp)
        {
            StartPeer(port, localPlayerId, hostIp, NetworkRole.Spectator);
        }

        public void Tick(float unscaledTime)
        {
            if (_udpClient == null)
            {
                return;
            }

            if ((_role == NetworkRole.Client || _role == NetworkRole.Spectator) &&
                !_connected &&
                unscaledTime - _lastHelloTime > 1f)
            {
                SendHello();
            }

            while (true)
            {
                LanMessage message = null;
                lock (_inboxLock)
                {
                    if (_inbox.Count > 0)
                    {
                        message = _inbox.Dequeue();
                    }
                }

                if (message == null)
                {
                    break;
                }

                MessageReceived?.Invoke(message);
            }
        }

        public int GetPeerCount(NetworkRole role)
        {
            if (_role != NetworkRole.Host)
            {
                return _connected && role == NetworkRole.Host ? 1 : 0;
            }

            var count = 0;
            foreach (var peer in _hostPeers.Values)
            {
                if (peer.role == role)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasPeer(NetworkRole role)
        {
            return GetPeerCount(role) > 0;
        }

        public void SendPose(PosePayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.Pose, JsonUtility.ToJson(payload)));
        }

        public void SendStartCalibration()
        {
            if (!_connected)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.StartCalibration, string.Empty));
        }

        public void SendWorldRootSync(WorldRootSyncPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.WorldRootSync, JsonUtility.ToJson(payload)));
        }

        public void SendShoot(ShootPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.Shoot, JsonUtility.ToJson(payload)));
        }

        public void SendShield(ShieldPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.Shield, JsonUtility.ToJson(payload)));
        }

        public void SendHpUpdate(HpUpdatePayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.HpUpdate, JsonUtility.ToJson(payload)));
        }

        public void SendMatchResult(MatchResultPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.MatchResult, JsonUtility.ToJson(payload)));
        }

        public void SendSharedAnchor(SharedAnchorPayload payload)
        {
            if (!_connected || payload == null || string.IsNullOrEmpty(payload.uuid))
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.SharedAnchor, JsonUtility.ToJson(payload)));
        }

        public void SendStartPlaying()
        {
            if (!_connected)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.StartPlaying, string.Empty));
        }

        public void SendCalibrationReady(CalibrationReadyPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.CalibrationReady, JsonUtility.ToJson(payload)));
        }

        public void SendRemoteAlignment(RemoteAlignmentPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.RemoteAlignment, JsonUtility.ToJson(payload)));
        }

        public void SendRematchReady(RematchReadyPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.RematchReady, JsonUtility.ToJson(payload)));
        }

        public void SendSpectatorVote(SpectatorVotePayload payload)
        {
            if (!_connected || payload == null || string.IsNullOrEmpty(payload.targetRole))
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.SpectatorVote, JsonUtility.ToJson(payload)));
        }

        public void SendObstacleSpawnRequest(ObstacleSpawnRequestPayload payload)
        {
            if (!_connected || payload == null || string.IsNullOrEmpty(payload.anchorType))
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.ObstacleSpawnRequest, JsonUtility.ToJson(payload)));
        }

        public void SendObstacleState(ObstacleStatePayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            Send(BuildMessage(LanMessageTypes.ObstacleState, JsonUtility.ToJson(payload)));
        }

        public void Stop()
        {
            _role = NetworkRole.None;
            _connected = false;
            _serverEndpoint = null;
            _lastHelloTime = 0f;
            _hostPeers.Clear();
            _lastDiagnostic = "Idle";

            if (_udpClient != null)
            {
                try
                {
                    _udpClient.Close();
                }
                catch
                {
                }

                _udpClient = null;
            }

            lock (_inboxLock)
            {
                _inbox.Clear();
            }
        }

        private void StartPeer(int port, string localPlayerId, string hostIp, NetworkRole role)
        {
            Stop();
            _localPlayerId = localPlayerId;
            _role = role;
            _connected = false;

            _udpClient = new UdpClient(0);
            _udpClient.EnableBroadcast = true;
            _serverEndpoint = new IPEndPoint(IPAddress.Parse(hostIp), port);
            BeginReceive();
            SetDiagnostic($"{_role} boot -> host {hostIp}:{port}");
            SendHello();
            Debug.Log($"M3 Net: {_role} started. Host={hostIp}:{port}");
        }

        private LanMessage BuildMessage(string type, string payload)
        {
            return new LanMessage
            {
                type = type,
                playerId = _localPlayerId,
                senderRole = _role.ToString(),
                payload = payload
            };
        }

        private void SendHello()
        {
            _lastHelloTime = Time.unscaledTime;
            SetDiagnostic($"{_role} sent HELLO -> {_serverEndpoint}");
            Send(BuildMessage(LanMessageTypes.Hello, string.Empty), _serverEndpoint);
        }

        private void BeginReceive()
        {
            if (_udpClient == null)
            {
                return;
            }

            try
            {
                _udpClient.BeginReceive(OnReceive, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"M3 Net: BeginReceive failed: {e.Message}");
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_udpClient == null)
            {
                return;
            }

            IPEndPoint remoteEndpoint = null;
            byte[] bytes;
            try
            {
                bytes = _udpClient.EndReceive(ar, ref remoteEndpoint);
            }
            catch
            {
                return;
            }

            BeginReceive();

            if (bytes == null || bytes.Length == 0 || remoteEndpoint == null)
            {
                return;
            }

            var json = Encoding.UTF8.GetString(bytes);
            LanMessage message;
            try
            {
                message = JsonUtility.FromJson<LanMessage>(json);
            }
            catch
            {
                return;
            }

            if (message == null || string.IsNullOrEmpty(message.type))
            {
                return;
            }

            HandleHandshake(message, remoteEndpoint);
            RelayIfNeeded(message, remoteEndpoint);
            lock (_inboxLock)
            {
                _inbox.Enqueue(message);
            }
        }

        private void HandleHandshake(LanMessage message, IPEndPoint remoteEndpoint)
        {
            // 握手阶段只处理 HELLO / HELLO_ACK，并在 Host 端记录已连入的角色端点。
            if (_role == NetworkRole.Host && message.type == LanMessageTypes.Hello)
            {
                var key = remoteEndpoint.ToString();
                _hostPeers[key] = new HostPeerInfo
                {
                    endpoint = remoteEndpoint,
                    role = ParseRole(message.senderRole)
                };

                SetDiagnostic($"Host received HELLO from {message.senderRole} @ {remoteEndpoint}");

                var ack = BuildMessage(LanMessageTypes.HelloAck, string.Empty);
                Send(ack, remoteEndpoint);
                SetConnected();
            }
            else if ((_role == NetworkRole.Client || _role == NetworkRole.Spectator) &&
                     message.type == LanMessageTypes.HelloAck)
            {
                _serverEndpoint = remoteEndpoint;
                SetDiagnostic($"{_role} received HELLO_ACK from {remoteEndpoint}");
                SetConnected();
            }
        }

        private void RelayIfNeeded(LanMessage message, IPEndPoint sourceEndpoint)
        {
            // Host 只中继需要跨端传播的消息，避免所有消息无差别广播。
            if (_role != NetworkRole.Host)
            {
                return;
            }

            if (message.type == LanMessageTypes.Hello || message.type == LanMessageTypes.HelloAck)
            {
                return;
            }

            var senderRole = ParseRole(message.senderRole);
            var shouldRelay = senderRole == NetworkRole.Client ||
                              (senderRole == NetworkRole.Spectator && message.type == LanMessageTypes.RemoteAlignment);
            if (!shouldRelay)
            {
                return;
            }

            SetDiagnostic($"Host relaying {message.type} from {message.senderRole}");
            SendToAllPeersExcept(message, sourceEndpoint);
        }

        private void SetConnected()
        {
            if (_connected)
            {
                return;
            }

            _connected = true;
            SetDiagnostic(_role == NetworkRole.Host
                ? $"Host connected. Peers={_hostPeers.Count}"
                : $"{_role} connected to {_serverEndpoint}");
            Connected?.Invoke();
        }

        private void SetDiagnostic(string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
            {
                return;
            }

            _lastDiagnostic = diagnostic;
        }

        private void Send(LanMessage message, IPEndPoint endpoint = null)
        {
            if (_udpClient == null || message == null)
            {
                return;
            }

            if (_role == NetworkRole.Host && endpoint == null)
            {
                foreach (var peer in _hostPeers.Values)
                {
                    SendToEndpoint(message, peer.endpoint);
                }

                return;
            }

            var target = endpoint ?? _serverEndpoint;
            if (target == null)
            {
                return;
            }

            SendToEndpoint(message, target);
        }

        private void SendToAllPeersExcept(LanMessage message, IPEndPoint excludedEndpoint)
        {
            foreach (var peer in _hostPeers.Values)
            {
                if (excludedEndpoint != null && EndpointsEqual(peer.endpoint, excludedEndpoint))
                {
                    continue;
                }

                SendToEndpoint(message, peer.endpoint);
            }
        }

        private void SendToEndpoint(LanMessage message, IPEndPoint endpoint)
        {
            if (_udpClient == null || message == null || endpoint == null)
            {
                return;
            }

            var json = JsonUtility.ToJson(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                _udpClient.Send(bytes, bytes.Length, endpoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"M3 Net: Send failed: {e.Message}");
            }
        }

        private static NetworkRole ParseRole(string value)
        {
            return Enum.TryParse(value, true, out NetworkRole parsed) ? parsed : NetworkRole.None;
        }

        private static bool EndpointsEqual(IPEndPoint a, IPEndPoint b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return Equals(a.Address, b.Address) && a.Port == b.Port;
        }
    }
}

