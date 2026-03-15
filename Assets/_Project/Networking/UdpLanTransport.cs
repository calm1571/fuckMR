using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Project.Networking
{
    public sealed class UdpLanTransport
    {
        private readonly Queue<LanMessage> _inbox = new Queue<LanMessage>();
        private readonly object _inboxLock = new object();

        private UdpClient _udpClient;
        private IPEndPoint _peerEndpoint;
        private IPEndPoint _hostEndpoint;
        private string _localPlayerId;
        private NetworkRole _role = NetworkRole.None;
        private float _lastHelloTime;
        private bool _connected;

        public event Action Connected;
        public event Action<LanMessage> MessageReceived;

        public bool IsConnected => _connected;
        public NetworkRole Role => _role;

        public void StartHost(int port, string localPlayerId)
        {
            Stop();
            _localPlayerId = localPlayerId;
            _role = NetworkRole.Host;
            _connected = false;

            _udpClient = new UdpClient(port);
            _udpClient.EnableBroadcast = true;
            BeginReceive();
            Debug.Log($"M3 Net: Host started on UDP {port}");
        }

        public void StartClient(int port, string localPlayerId, string hostIp)
        {
            Stop();
            _localPlayerId = localPlayerId;
            _role = NetworkRole.Client;
            _connected = false;

            _udpClient = new UdpClient(0);
            _udpClient.EnableBroadcast = true;
            _hostEndpoint = new IPEndPoint(IPAddress.Parse(hostIp), port);
            BeginReceive();
            SendHello();
            Debug.Log($"M3 Net: Client started. Host={hostIp}:{port}");
        }

        public void Tick(float unscaledTime)
        {
            if (_udpClient == null)
            {
                return;
            }

            if (_role == NetworkRole.Client && !_connected && unscaledTime - _lastHelloTime > 1f)
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

        public void SendPose(PosePayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.Pose,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendStartCalibration()
        {
            if (!_connected)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.StartCalibration,
                playerId = _localPlayerId,
                payload = string.Empty
            };

            Send(message);
        }

        public void SendWorldRootSync(WorldRootSyncPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.WorldRootSync,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendShoot(ShootPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.Shoot,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendShield(ShieldPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.Shield,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendHpUpdate(HpUpdatePayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.HpUpdate,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendMatchResult(MatchResultPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.MatchResult,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendSharedAnchor(SharedAnchorPayload payload)
        {
            if (!_connected || payload == null || string.IsNullOrEmpty(payload.uuid))
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.SharedAnchor,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendStartPlaying()
        {
            if (!_connected)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.StartPlaying,
                playerId = _localPlayerId,
                payload = string.Empty
            };

            Send(message);
        }

        public void SendCalibrationReady(CalibrationReadyPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.CalibrationReady,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendRemoteAlignment(RemoteAlignmentPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.RemoteAlignment,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void SendRematchReady(RematchReadyPayload payload)
        {
            if (!_connected || payload == null)
            {
                return;
            }

            var message = new LanMessage
            {
                type = LanMessageTypes.RematchReady,
                playerId = _localPlayerId,
                payload = JsonUtility.ToJson(payload)
            };

            Send(message);
        }

        public void Stop()
        {
            _role = NetworkRole.None;
            _connected = false;
            _peerEndpoint = null;
            _hostEndpoint = null;
            _lastHelloTime = 0f;

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

        private void SendHello()
        {
            _lastHelloTime = Time.unscaledTime;
            var message = new LanMessage
            {
                type = LanMessageTypes.Hello,
                playerId = _localPlayerId,
                payload = string.Empty
            };

            Send(message, _hostEndpoint);
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
            lock (_inboxLock)
            {
                _inbox.Enqueue(message);
            }
        }

        private void HandleHandshake(LanMessage message, IPEndPoint remoteEndpoint)
        {
            if (_role == NetworkRole.Host && message.type == LanMessageTypes.Hello)
            {
                if (_peerEndpoint == null)
                {
                    _peerEndpoint = remoteEndpoint;
                    var ack = new LanMessage
                    {
                        type = LanMessageTypes.HelloAck,
                        playerId = _localPlayerId,
                        payload = string.Empty
                    };
                    Send(ack, _peerEndpoint);
                    SetConnected();
                }
            }
            else if (_role == NetworkRole.Client && message.type == LanMessageTypes.HelloAck)
            {
                _peerEndpoint = remoteEndpoint;
                SetConnected();
            }
        }

        private void SetConnected()
        {
            if (_connected)
            {
                return;
            }

            _connected = true;
            Connected?.Invoke();
        }

        private void Send(LanMessage message, IPEndPoint endpoint = null)
        {
            if (_udpClient == null || message == null)
            {
                return;
            }

            var target = endpoint ?? _peerEndpoint;
            if (target == null)
            {
                return;
            }

            var json = JsonUtility.ToJson(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                _udpClient.Send(bytes, bytes.Length, target);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"M3 Net: Send failed: {e.Message}");
            }
        }
    }
}
