using UnityEngine;
using UnityEngine.XR;

namespace Project.MRWorld
{
    public sealed class RemoteAlignmentController
    {
        private readonly Transform _offsetRoot;
        private readonly float _moveSpeed;
        private readonly float _rotateSpeed;
        private readonly float _heightSpeed;

        private InputDevice _leftController;
        private InputDevice _rightController;
        private Transform _pivotTransform;

        public RemoteAlignmentController(Transform offsetRoot, float moveSpeed, float rotateSpeed, float heightSpeed)
        {
            _offsetRoot = offsetRoot;
            _moveSpeed = Mathf.Max(0.05f, moveSpeed);
            _rotateSpeed = Mathf.Clamp(rotateSpeed * 0.64f, 20f, 48f);
            _heightSpeed = Mathf.Max(0.02f, heightSpeed);
        }

        public void SetPivotTransform(Transform pivotTransform)
        {
            _pivotTransform = pivotTransform;
        }

        public void Tick(float deltaTime)
        {
            if (_offsetRoot == null)
            {
                return;
            }

            EnsureDevices();

            var rightAxis = Vector2.zero;
            var xPressed = false;
            var yPressed = false;
            var aPressed = false;
            var bPressed = false;

            if (_leftController.isValid)
            {
                _leftController.TryGetFeatureValue(CommonUsages.primaryButton, out xPressed);
                _leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);
            }

            if (_rightController.isValid)
            {
                _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightAxis);
                _rightController.TryGetFeatureValue(CommonUsages.primaryButton, out aPressed);
                _rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bPressed);
            }

            ApplyTranslation(rightAxis, deltaTime);
            ApplyRotation(xPressed, yPressed, deltaTime);
            ApplyHeight(aPressed, bPressed, deltaTime);
        }

        public string BuildStatusText()
        {
            if (_offsetRoot == null)
            {
                return "Remote Offset: null";
            }

            var pos = _offsetRoot.position;
            var yaw = _offsetRoot.eulerAngles.y;
            return $"Remote Offset: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}\nRemote Yaw Offset: {yaw:F1}\nRight Stick: Move proxy / Hold X or Y: Rotate Around Visible Head / A/B: Height";
        }

        private void EnsureDevices()
        {
            if (!_leftController.isValid)
            {
                _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            if (!_rightController.isValid)
            {
                _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }
        }

        private void ApplyTranslation(Vector2 axis, float deltaTime)
        {
            if (axis.sqrMagnitude < 0.0004f)
            {
                return;
            }

            var forwardRef = _offsetRoot.forward;
            var rightRef = _offsetRoot.right;
            var flatForward = Vector3.ProjectOnPlane(forwardRef, Vector3.up).normalized;
            var flatRight = Vector3.ProjectOnPlane(rightRef, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            if (flatRight.sqrMagnitude < 0.001f)
            {
                flatRight = Vector3.right;
            }

            _offsetRoot.position += (flatRight * axis.x + flatForward * axis.y) * (_moveSpeed * deltaTime);
        }

        private void ApplyRotation(bool xPressed, bool yPressed, float deltaTime)
        {
            var signedInput = 0f;
            if (xPressed)
            {
                signedInput += 1f;
            }

            if (yPressed)
            {
                signedInput -= 1f;
            }

            if (Mathf.Abs(signedInput) < 0.001f)
            {
                return;
            }

            var deltaYaw = signedInput * (_rotateSpeed * deltaTime);
            if (_pivotTransform != null)
            {
                // RotateAround already updates both root position and rotation around the visible head pivot.
                _offsetRoot.RotateAround(_pivotTransform.position, Vector3.up, deltaYaw);
                return;
            }

            _offsetRoot.Rotate(0f, deltaYaw, 0f, Space.Self);
        }

        private void ApplyHeight(bool aPressed, bool bPressed, float deltaTime)
        {
            var delta = 0f;
            if (aPressed)
            {
                delta += _heightSpeed * deltaTime;
            }

            if (bPressed)
            {
                delta -= _heightSpeed * deltaTime;
            }

            if (Mathf.Abs(delta) < 0.00001f)
            {
                return;
            }

            var pos = _offsetRoot.position;
            pos.y += delta;
            _offsetRoot.position = pos;
        }
    }
}
