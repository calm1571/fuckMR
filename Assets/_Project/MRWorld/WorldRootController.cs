// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Controls the runtime world root transform used for alignment.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;
using UnityEngine.XR;

namespace Project.MRWorld
{
        /// <summary>
    /// Manages the movable WorldRoot transform.
    /// </summary>
    public sealed class WorldRootController
    {
        private readonly Transform _worldRoot;
        private readonly Transform _referenceForward;
        private readonly float _moveSpeed;
        private readonly float _rotateSpeed;
        private readonly float _heightSpeed;

        private InputDevice _leftController;
        private InputDevice _rightController;

        public WorldRootController(Transform worldRoot, Transform referenceForward, float moveSpeed, float rotateSpeed, float heightSpeed)
        {
            _worldRoot = worldRoot;
            _referenceForward = referenceForward;
            _moveSpeed = Mathf.Max(0.05f, moveSpeed);
            _rotateSpeed = Mathf.Max(5f, rotateSpeed);
            _heightSpeed = Mathf.Max(0.02f, heightSpeed);
        }

        public void Tick(float deltaTime)
        {
            if (_worldRoot == null)
            {
                return;
            }

            EnsureDevices();

            var leftAxis = Vector2.zero;
            var rightAxis = Vector2.zero;
            var aPressed = false;
            var bPressed = false;

            if (_leftController.isValid)
            {
                _leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftAxis);
            }

            if (_rightController.isValid)
            {
                _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightAxis);
                _rightController.TryGetFeatureValue(CommonUsages.primaryButton, out aPressed);
                _rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bPressed);
            }

            ApplyTranslation(rightAxis, deltaTime);
            ApplyRotation(leftAxis.x, deltaTime);
            ApplyHeight(aPressed, bPressed, deltaTime);
        }

        public string BuildStatusText()
        {
            if (_worldRoot == null)
            {
                return "WorldRoot: null";
            }

            var pos = _worldRoot.position;
            var yaw = _worldRoot.eulerAngles.y;
            return $"WorldRoot Pos: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}\nYaw: {yaw:F1}\nA/B: Height +/-";
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

            var forwardRef = _referenceForward != null ? _referenceForward.forward : Vector3.forward;
            var rightRef = _referenceForward != null ? _referenceForward.right : Vector3.right;
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

            var move = (flatRight * axis.x + flatForward * axis.y) * (_moveSpeed * deltaTime);
            _worldRoot.position += move;
        }

        private void ApplyRotation(float axisX, float deltaTime)
        {
            if (Mathf.Abs(axisX) < 0.15f)
            {
                return;
            }

            var yaw = axisX * (_rotateSpeed * deltaTime);
            _worldRoot.Rotate(Vector3.up, yaw, Space.World);
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

            var pos = _worldRoot.position;
            pos.y += delta;
            _worldRoot.position = pos;
        }
    }
}




