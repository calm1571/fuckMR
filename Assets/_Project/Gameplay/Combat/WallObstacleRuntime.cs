using UnityEngine;

namespace Project.Gameplay.Combat
{
    public sealed class WallObstacleRuntime
    {
        private readonly GameObject _root;
        private readonly Transform _barRoot;
        private readonly Transform _barFill;
        private readonly Renderer _wallRenderer;
        private readonly Renderer _barBackgroundRenderer;
        private readonly Renderer _barFillRenderer;
        private readonly BoxCollider _collider;
        private readonly bool _isPreview;
        private readonly Vector3 _size;

        private float _maxHp = 1f;
        private float _currentHp = 1f;

        public int ObstacleId { get; }
        public Transform Transform => _root.transform;
        public Collider Collider => _collider;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public Vector3 Size => _size;

        public WallObstacleRuntime(string name, int obstacleId, Transform parent, Vector3 size, bool isPreview)
        {
            ObstacleId = obstacleId;
            _isPreview = isPreview;
            _size = new Vector3(
                Mathf.Max(0.2f, size.x),
                Mathf.Max(0.2f, size.y),
                Mathf.Max(0.02f, size.z));

            _root = new GameObject(name);
            if (parent != null)
            {
                _root.transform.SetParent(parent, false);
            }

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(_root.transform, false);
            wall.transform.localPosition = Vector3.zero;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = _size;
            _wallRenderer = wall.GetComponent<Renderer>();
            _collider = wall.GetComponent<BoxCollider>();
            if (_collider != null)
            {
                _collider.enabled = !isPreview;
            }

            if (_wallRenderer != null)
            {
                _wallRenderer.material = BuildWallMaterial(isPreview);
            }

            _barRoot = new GameObject("HpBarRoot").transform;
            _barRoot.SetParent(_root.transform, false);
            _barRoot.localPosition = new Vector3(0f, _size.y * 0.65f + 0.14f, 0f);
            _barRoot.localRotation = Quaternion.identity;

            var background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(_barRoot, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(Mathf.Max(0.4f, _size.x * 0.78f), 0.05f, 0.02f);
            _barBackgroundRenderer = background.GetComponent<Renderer>();
            var backgroundCollider = background.GetComponent<Collider>();
            if (backgroundCollider != null)
            {
                backgroundCollider.enabled = false;
            }

            if (_barBackgroundRenderer != null)
            {
                _barBackgroundRenderer.material = BuildFlatMaterial(new Color(0.03f, 0.03f, 0.03f, isPreview ? 0.45f : 0.95f), isPreview);
            }

            _barFill = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            _barFill.name = "Fill";
            _barFill.SetParent(_barRoot, false);
            _barFill.localPosition = new Vector3(0f, 0f, -0.012f);
            _barFill.localScale = new Vector3(Mathf.Max(0.4f, _size.x * 0.74f), 0.032f, 0.012f);
            _barFillRenderer = _barFill.GetComponent<Renderer>();
            var fillCollider = _barFill.GetComponent<Collider>();
            if (fillCollider != null)
            {
                fillCollider.enabled = false;
            }

            if (_barFillRenderer != null)
            {
                _barFillRenderer.material = BuildFlatMaterial(isPreview ? new Color(0.2f, 0.7f, 1f, 0.5f) : new Color(0.9f, 0.12f, 0.12f, 1f), isPreview);
            }

            _barRoot.gameObject.SetActive(!isPreview);
            SetHp(1f, 1f);
        }

        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            _root.transform.SetPositionAndRotation(position, rotation);
        }

        public void SetHp(float currentHp, float maxHp)
        {
            _maxHp = Mathf.Max(1f, maxHp);
            _currentHp = Mathf.Clamp(currentHp, 0f, _maxHp);
            if (_isPreview)
            {
                return;
            }

            var normalized = Mathf.Clamp01(_currentHp / _maxHp);
            var backgroundWidth = Mathf.Max(0.4f, _size.x * 0.78f);
            var fillWidth = Mathf.Max(0f, backgroundWidth - 0.04f) * normalized;
            var fillScale = _barFill.localScale;
            fillScale.x = Mathf.Max(0.0001f, fillWidth);
            _barFill.localScale = fillScale;
            _barFill.localPosition = new Vector3(-(backgroundWidth - fillWidth) * 0.5f, 0f, -0.012f);
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

        public void SetPreviewVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void LookHpBarAt(Vector3 lookTarget)
        {
            if (_isPreview || _barRoot == null)
            {
                return;
            }

            var flatForward = Vector3.ProjectOnPlane(lookTarget - _barRoot.position, Vector3.up).normalized;
            if (flatForward.sqrMagnitude > 0.0001f)
            {
                _barRoot.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            }
        }

        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }

        private static Material BuildWallMaterial(bool isPreview)
        {
            var color = isPreview ? new Color(0.2f, 0.75f, 1f, 0.28f) : new Color(0.46f, 0.67f, 0.85f, 0.92f);
            return BuildFlatMaterial(color, isPreview);
        }

        private static Material BuildFlatMaterial(Color color, bool transparent)
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

            var material = new Material(shader)
            {
                color = color
            };

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return material;
        }
    }
}
