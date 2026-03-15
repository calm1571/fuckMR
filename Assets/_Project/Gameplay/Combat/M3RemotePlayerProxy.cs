using Project.Networking;
using UnityEngine;

namespace Project.Gameplay.Combat
{
    public sealed class M3RemotePlayerProxy : MonoBehaviour
    {
        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _hpBarRoot;
        private Transform _hpBarBackground;
        private Transform _hpBarForeground;
        private Material _hpBgMat;
        private Material _hpFgMat;
        private bool _initialized;
        private float _lastHealth01 = 1f;
        private const float HpBarWidth = 0.28f;
        private const float HpBarHeight = 0.028f;
        private const float HpBarDepth = 0.012f;
        private const float HpBarYOffset = 0.20f;
        private const float HpBarFrontOffsetLocal = 0.52f;

        public Transform HeadTransform => _head;
        public Transform LeftHandTransform => _leftHand;
        public Transform RightHandTransform => _rightHand;

        private void Awake()
        {
            EnsureProxyObjects();
            SetVisible(false);
        }

        public void ApplyPose(PosePayload pose)
        {
            if (pose == null)
            {
                return;
            }

            EnsureProxyObjects();
            Apply(_head, pose.head);
            Apply(_leftHand, pose.leftHand);
            Apply(_rightHand, pose.rightHand);
            UpdateHpBarTransform();
            SetVisible(true);
        }

        public void SetEnemyHealthNormalized(float normalizedHealth)
        {
            _lastHealth01 = Mathf.Clamp01(normalizedHealth);
            if (!_initialized || _hpBarForeground == null)
            {
                return;
            }

            var scaleX = Mathf.Max(0f, _lastHealth01);
            _hpBarForeground.localScale = new Vector3(scaleX, 1f, 1f);
            _hpBarForeground.localPosition = new Vector3((scaleX - 1f) * 0.5f, 0f, HpBarFrontOffsetLocal);
        }

        private void EnsureProxyObjects()
        {
            if (_initialized)
            {
                return;
            }

            _head = CreatePart("RemoteHead", PrimitiveType.Sphere, new Vector3(0.20f, 0.20f, 0.20f), new Color(0.2f, 1f, 0.45f, 0.95f));
            _leftHand = CreatePart("RemoteLeftHand", PrimitiveType.Cube, new Vector3(0.08f, 0.08f, 0.08f), new Color(0.1f, 0.78f, 1f, 0.95f));
            _rightHand = CreatePart("RemoteRightHand", PrimitiveType.Cube, new Vector3(0.08f, 0.08f, 0.08f), new Color(0.1f, 0.78f, 1f, 0.95f));
            CreateHpBar();
            SetEnemyHealthNormalized(_lastHealth01);
            _initialized = true;
        }

        private void CreateHpBar()
        {
            if (_head == null || _hpBarRoot != null)
            {
                return;
            }

            _hpBarRoot = new GameObject("EnemyHpBarRoot").transform;
            _hpBarRoot.SetParent(transform, false);
            _hpBarRoot.position = _head.position + Vector3.up * HpBarYOffset;
            _hpBarRoot.rotation = Quaternion.identity;
            _hpBarRoot.localScale = Vector3.one;

            _hpBarBackground = CreateHpPart(
                "HpBarBg",
                _hpBarRoot,
                new Vector3(0f, 0f, 0f),
                new Vector3(HpBarWidth, HpBarHeight, HpBarDepth),
                ref _hpBgMat,
                new Color(0f, 0f, 0f, 1f));

            _hpBarForeground = CreateHpPart(
                "HpBarFg",
                _hpBarBackground,
                new Vector3(0f, 0f, HpBarFrontOffsetLocal),
                Vector3.one,
                ref _hpFgMat,
                new Color(0.92f, 0.08f, 0.08f, 1f));
        }

        private Transform CreatePart(string name, PrimitiveType primitive, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = color;
                    renderer.material = mat;
                }
            }

            return go.transform;
        }

        private static Transform CreateHpPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, ref Material cacheMat, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (cacheMat == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null)
                    {
                        shader = Shader.Find("Unlit/Color");
                    }

                    if (shader != null)
                    {
                        cacheMat = new Material(shader);
                        cacheMat.color = color;
                    }
                }

                if (cacheMat != null)
                {
                    renderer.material = cacheMat;
                }
            }

            return go.transform;
        }

        private static void Apply(Transform target, PoseData pose)
        {
            if (target == null)
            {
                return;
            }

            target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void UpdateHpBarTransform()
        {
            if (_hpBarRoot == null || _head == null || Camera.main == null)
            {
                return;
            }

            _hpBarRoot.position = _head.position + Vector3.up * HpBarYOffset;
            var toCam = Camera.main.transform.position - _hpBarRoot.position;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _hpBarRoot.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        private void SetVisible(bool visible)
        {
            if (_head != null)
            {
                _head.gameObject.SetActive(visible);
            }

            if (_leftHand != null)
            {
                _leftHand.gameObject.SetActive(visible);
            }

            if (_rightHand != null)
            {
                _rightHand.gameObject.SetActive(visible);
            }

            if (_hpBarRoot != null)
            {
                _hpBarRoot.gameObject.SetActive(visible);
            }
        }
    }
}
