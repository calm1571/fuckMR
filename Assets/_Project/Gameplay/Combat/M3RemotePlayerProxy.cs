// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Builds and updates remote player avatar visuals and markers.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using Project.Networking;
using UnityEngine;

namespace Project.Gameplay.Combat
{
        /// <summary>
    /// 远端玩家视觉代理，包含头、手、标识与血条。
    /// </summary>
    public sealed class M3RemotePlayerProxy : MonoBehaviour
    {
        private Transform _alignmentRoot;
        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _headForwardMarkerRoot;
        private Transform _leftHandLabelRoot;
        private Transform _rightHandLabelRoot;
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
        private const float HeadMarkerFrontOffset = 0.17f;

        public Transform HeadTransform => _head;
        public Transform LeftHandTransform => _leftHand;
        public Transform RightHandTransform => _rightHand;

        private void LateUpdate()
        {
            UpdateHeadForwardMarkerTransform();
            UpdateHpBarTransform();
            UpdateHandLabelTransforms();
        }

        private void Awake()
        {
            EnsureProxyObjects();
            SetVisible(false);
        }

        public void BindAlignmentRoot(Transform alignmentRoot)
        {
            _alignmentRoot = alignmentRoot;
        }

        public void ApplyPose(PosePayload pose)
        {
            if (pose == null)
            {
                return;
            }

            EnsureProxyObjects();
            Apply(_head, pose.head, _alignmentRoot);
            Apply(_leftHand, pose.leftHand, _alignmentRoot);
            Apply(_rightHand, pose.rightHand, _alignmentRoot);
            UpdateHeadForwardMarkerTransform();
            UpdateHandLabelTransforms();
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

        public void Hide()
        {
            SetVisible(false);
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
            CreateHeadForwardMarker();
            _leftHandLabelRoot = CreateHandLabel(_leftHand, "L", new Color(0.95f, 0.96f, 1f, 1f));
            _rightHandLabelRoot = CreateHandLabel(_rightHand, "R", new Color(0.95f, 0.96f, 1f, 1f));
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

        private void CreateHeadForwardMarker()
        {
            if (_head == null)
            {
                return;
            }

            _headForwardMarkerRoot = new GameObject("HeadForwardMarkerRoot").transform;
            _headForwardMarkerRoot.SetParent(transform, false);
            _headForwardMarkerRoot.localScale = Vector3.one;

            CreateMarkerBar("HeadForwardPlusBgVertical", _headForwardMarkerRoot, Vector3.zero, new Vector3(0.032f, 0.126f, 0.014f), new Color(0f, 0f, 0f, 0.98f));
            CreateMarkerBar("HeadForwardPlusBgHorizontal", _headForwardMarkerRoot, Vector3.zero, new Vector3(0.126f, 0.032f, 0.014f), new Color(0f, 0f, 0f, 0.98f));
            CreateMarkerBar("HeadForwardPlusVertical", _headForwardMarkerRoot, new Vector3(0f, 0f, 0.002f), new Vector3(0.016f, 0.094f, 0.012f), new Color(1f, 1f, 1f, 0.99f));
            CreateMarkerBar("HeadForwardPlusHorizontal", _headForwardMarkerRoot, new Vector3(0f, 0f, 0.002f), new Vector3(0.094f, 0.016f, 0.012f), new Color(1f, 1f, 1f, 0.99f));
            UpdateHeadForwardMarkerTransform();
        }

        private static void CreateMarkerBar(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = localScale;

            var collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = bar.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return;
            }

            var material = new Material(shader);
            material.color = color;
            renderer.material = material;
        }

        private static Transform CreateHandLabel(Transform hand, string label, Color color)
        {
            if (hand == null)
            {
                return null;
            }

            var labelRoot = new GameObject(hand.name + "Label");
            labelRoot.transform.SetParent(hand.root != null ? hand.root : hand, false);
            labelRoot.transform.position = hand.position + Vector3.up * 0.10f;
            labelRoot.transform.rotation = Quaternion.identity;
            labelRoot.transform.localScale = Vector3.one * 0.045f;

            var textMesh = labelRoot.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 120;
            textMesh.characterSize = 0.55f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;
            return labelRoot.transform;
        }

        private void UpdateHandLabelTransforms()
        {
            UpdateHandLabelTransform(_leftHand, _leftHandLabelRoot);
            UpdateHandLabelTransform(_rightHand, _rightHandLabelRoot);
        }

        private void UpdateHeadForwardMarkerTransform()
        {
            if (_head == null || _headForwardMarkerRoot == null)
            {
                return;
            }

            _headForwardMarkerRoot.position = _head.position + _head.forward * HeadMarkerFrontOffset;
            _headForwardMarkerRoot.rotation = _head.rotation;
        }

        private static void UpdateHandLabelTransform(Transform hand, Transform labelRoot)
        {
            if (hand == null || labelRoot == null || Camera.main == null)
            {
                return;
            }

            labelRoot.position = hand.position + Vector3.up * 0.10f;
            var toCam = Camera.main.transform.position - labelRoot.position;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                return;
            }

            labelRoot.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        private void Apply(Transform target, PoseData pose, Transform alignmentRoot)
        {
            if (target == null)
            {
                return;
            }

            var position = pose.position;
            var rotation = pose.rotation;
            if (alignmentRoot != null)
            {
                position = alignmentRoot.TransformPoint(position);
                rotation = alignmentRoot.rotation * rotation;
            }

            target.SetPositionAndRotation(position, rotation);
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

            if (_leftHandLabelRoot != null)
            {
                _leftHandLabelRoot.gameObject.SetActive(visible);
            }

            if (_rightHandLabelRoot != null)
            {
                _rightHandLabelRoot.gameObject.SetActive(visible);
            }

            if (_headForwardMarkerRoot != null)
            {
                _headForwardMarkerRoot.gameObject.SetActive(visible);
            }

            if (_hpBarRoot != null)
            {
                _hpBarRoot.gameObject.SetActive(visible);
            }
        }
    }
}



