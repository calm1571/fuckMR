using Project.Gameplay.Input;
using UnityEngine;
using UnityEngine.XR;

namespace Project.Gameplay.Combat
{
    public sealed class M1ProjectileShooter : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileLifetime = 6f;
        [SerializeField] private float projectileMaxDistance = 25f;
        [SerializeField] private float projectileRadius = 0.033f;
        [SerializeField] private bool addDebugTrail = true;

        [Header("Spawn")]
        [SerializeField] private float muzzleOffset = 0.08f;
        [SerializeField] private Transform shootOriginOverride;
        [SerializeField] private bool requireExplicitShootOrigin = true;

        private IPlayerInputSource _inputSource;
        private InputDevice _rightHandDevice;
        private Material _runtimeProjectileMaterial;
        private bool _isShootingEnabled;
        private bool _missingOriginLogged;

        public bool HasShootOriginAssigned => shootOriginOverride != null;

        public void SetShootOrigin(Transform origin)
        {
            shootOriginOverride = origin;
            _missingOriginLogged = false;
        }

        public void SetShootingEnabled(bool enabled)
        {
            _isShootingEnabled = enabled;
        }

        public void Bind(IPlayerInputSource inputSource)
        {
            Unbind();

            _inputSource = inputSource;
            if (_inputSource == null)
            {
                return;
            }

            _inputSource.TriggerDown += OnTriggerDown;
        }

        public void Unbind()
        {
            if (_inputSource != null)
            {
                _inputSource.TriggerDown -= OnTriggerDown;
                _inputSource = null;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnTriggerDown()
        {
            if (!_isShootingEnabled)
            {
                return;
            }

            if (!TryGetShootPose(out var position, out var rotation))
            {
                return;
            }

            var direction = rotation * Vector3.forward;
            var spawnPos = position + direction * muzzleOffset;

            var projectile = CreateProjectileObject();
            projectile.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(direction, Vector3.up));
            Debug.Log($"M1 Shoot: spawn={spawnPos} dir={direction}");

            var mover = projectile.GetComponent<M1Projectile>();
            if (mover == null)
            {
                mover = projectile.AddComponent<M1Projectile>();
            }

            mover.Initialize(direction, projectileSpeed, projectileMaxDistance, projectileLifetime);
        }

        private bool TryGetShootPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (shootOriginOverride != null)
            {
                position = shootOriginOverride.position;
                rotation = shootOriginOverride.rotation;
                return true;
            }

            if (requireExplicitShootOrigin)
            {
                if (!_missingOriginLogged)
                {
                    Debug.LogError("M1ProjectileShooter: shootOriginOverride is not assigned. Bind Right Controller transform in scene.");
                    _missingOriginLogged = true;
                }

                return false;
            }

            if (!_rightHandDevice.isValid)
            {
                _rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            if (_rightHandDevice.isValid &&
                _rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                _rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                return true;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            position = cam.transform.position;
            rotation = cam.transform.rotation;
            return true;
        }

        private GameObject CreateProjectileObject()
        {
            if (projectilePrefab != null)
            {
                return Instantiate(projectilePrefab);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "M1Projectile";
            var diameter = Mathf.Max(0.24f, projectileRadius * 2f);
            go.transform.localScale = Vector3.one * diameter;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (_runtimeProjectileMaterial == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null)
                    {
                        shader = Shader.Find("Unlit/Color");
                    }

                    if (shader == null)
                    {
                        shader = Shader.Find("Standard");
                    }

                    _runtimeProjectileMaterial = new Material(shader);
                    _runtimeProjectileMaterial.color = new Color(0.2f, 0.9f, 1f, 1f);
                    _runtimeProjectileMaterial.EnableKeyword("_EMISSION");
                    _runtimeProjectileMaterial.SetColor("_EmissionColor", new Color(0.15f, 1.8f, 2.2f, 1f));
                }

                renderer.material = _runtimeProjectileMaterial;
            }

            if (addDebugTrail)
            {
                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.22f;
                trail.startWidth = diameter * 0.55f;
                trail.endWidth = diameter * 0.1f;
                trail.minVertexDistance = 0.01f;
                trail.material = _runtimeProjectileMaterial;
                trail.startColor = new Color(0.4f, 1f, 1f, 0.95f);
                trail.endColor = new Color(0.4f, 1f, 1f, 0f);
            }

            return go;
        }
    }
}
