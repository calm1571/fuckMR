using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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

        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;

        public event Action RemoteCalibrationRequested;

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
        }

        public void StartClient(string hostIp = null)
        {
            Role = NetworkRole.Client;
            _transport.StartClient(_port, _localPlayerId, string.IsNullOrEmpty(hostIp) ? _defaultHostIp : hostIp);
            _hasRemotePose = false;
            _remoteCalibrationRequested = false;
        }

        public void Stop()
        {
            Role = NetworkRole.None;
            _transport.Stop();
            _hasRemotePose = false;
            _remoteCalibrationRequested = false;
        }

        public void Tick(float unscaledTime)
        {
            _transport.Tick(unscaledTime);

            if (_remoteCalibrationRequested)
            {
                _remoteCalibrationRequested = false;
                RemoteCalibrationRequested?.Invoke();
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
                _remoteCalibrationRequested = true;
            }
        }
    }
}
