using UnityEngine;
using UnityEngine.XR;

namespace Project.MRWorld
{
    // Manual fallback for marker alignment: move controller tip to printed tag center and press X to lock.
    public sealed class ManualMarkerTrackingSource : IMarkerTrackingSource
    {
        private readonly XRNode _poseNode;
        private readonly XRNode _lockInputNode;
        private readonly float _forwardOffset;
        private readonly float _stabilityThreshold;
        private readonly float _stabilityLerp;

        private InputDevice _poseDevice;
        private InputDevice _lockInputDevice;
        private Pose _smoothedPose;
        private Pose _lastPose;
        private bool _hasPose;
        private bool _isLocked;
        private float _stability01;
        private bool _xPrev;
        private bool _yPrev;

        public ManualMarkerTrackingSource(
            XRNode poseNode = XRNode.RightHand,
            XRNode lockInputNode = XRNode.LeftHand,
            float forwardOffset = 0.04f,
            float stabilityThreshold = 0.03f,
            float stabilityLerp = 5f)
        {
            _poseNode = poseNode;
            _lockInputNode = lockInputNode;
            _forwardOffset = Mathf.Max(0f, forwardOffset);
            _stabilityThreshold = Mathf.Clamp(stabilityThreshold, 0.001f, 0.2f);
            _stabilityLerp = Mathf.Max(0.1f, stabilityLerp);
        }

        public void Begin()
        {
            _poseDevice = InputDevices.GetDeviceAtXRNode(_poseNode);
            _lockInputDevice = InputDevices.GetDeviceAtXRNode(_lockInputNode);
            _hasPose = false;
            _isLocked = false;
            _stability01 = 0f;
            _xPrev = false;
            _yPrev = false;
        }

        public void End()
        {
        }

        public void Tick(float deltaTime)
        {
            EnsureDevices();
            var xPressed = false;
            var yPressed = false;
            if (_lockInputDevice.isValid)
            {
                _lockInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out xPressed);
                _lockInputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);
            }

            if (_poseDevice.isValid &&
                _poseDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var p) &&
                _poseDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var r))
            {
                var rawPose = new Pose(p + (r * Vector3.forward * _forwardOffset), r);
                if (!_hasPose)
                {
                    _smoothedPose = rawPose;
                    _lastPose = rawPose;
                    _hasPose = true;
                }
                else if (!_isLocked)
                {
                    var t = 1f - Mathf.Exp(-_stabilityLerp * Mathf.Max(0.0001f, deltaTime));
                    _smoothedPose.position = Vector3.Lerp(_smoothedPose.position, rawPose.position, t);
                    _smoothedPose.rotation = Quaternion.Slerp(_smoothedPose.rotation, rawPose.rotation, t);
                }

                var movement = Vector3.Distance(rawPose.position, _lastPose.position);
                var instantStability = Mathf.Clamp01(1f - movement / _stabilityThreshold);
                _stability01 = Mathf.Lerp(_stability01, instantStability, 0.2f);
                _lastPose = rawPose;
            }
            else
            {
                _hasPose = false;
                _stability01 = Mathf.Lerp(_stability01, 0f, 0.25f);
            }

            var xDown = xPressed && !_xPrev;
            var yDown = yPressed && !_yPrev;
            _xPrev = xPressed;
            _yPrev = yPressed;

            if (xDown && _hasPose)
            {
                _isLocked = true;
            }

            if (yDown)
            {
                _isLocked = false;
            }
        }

        public bool TryGetSample(out MarkerTrackingSample sample)
        {
            sample = new MarkerTrackingSample
            {
                hasPose = _hasPose,
                isLocked = _isLocked,
                stability01 = Mathf.Clamp01(_stability01),
                pose = _smoothedPose,
                sourceMode = MarkerTrackingSourceMode.Manual
            };
            return _hasPose;
        }

        public string BuildDebugText()
        {
            var state = _hasPose ? "Marker Pose Ready" : "Marker Pose Missing";
            var lockState = _isLocked ? "Locked" : "Unlocked";
            return $"Marker: {state} | {lockState} | Stability:{_stability01 * 100f:F0}%\nX Lock / Y Unlock";
        }

        private void EnsureDevices()
        {
            if (!_poseDevice.isValid)
            {
                _poseDevice = InputDevices.GetDeviceAtXRNode(_poseNode);
            }

            if (!_lockInputDevice.isValid)
            {
                _lockInputDevice = InputDevices.GetDeviceAtXRNode(_lockInputNode);
            }
        }
    }
}
