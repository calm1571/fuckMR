// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Builds and updates runtime wall obstacles, HP bars, and damage visuals.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.Gameplay.Combat
{
        /// <summary>
    /// 运行时障碍墙实体，包含模型、血条、裂痕和碰撞。
    /// </summary>
    public sealed class WallObstacleRuntime
    {
        private const int CrackCountPerFace = 4;
        private const int BranchCrackCountPerFace = 3;
        private const int ChipCountPerFace = 2;

        private readonly GameObject _root;
        private readonly Transform _barRoot;
        private readonly Transform _barFill;
        private readonly Transform _crackRoot;
        private readonly Renderer _wallRenderer;
        private readonly Renderer _barBackgroundRenderer;
        private readonly Renderer _barFillRenderer;
        private readonly Renderer[] _crackRenderers;
        private readonly Renderer[] _branchCrackRenderers;
        private readonly Renderer[] _chipRenderers;
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
            // 一次性构建墙本体、血条、裂痕和碰撞体；预览模式只保留视觉，不参与碰撞和血条显示。
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
            if (wall.GetComponent<WallObstacleColliderTag>() == null)
            {
                wall.AddComponent<WallObstacleColliderTag>();
            }
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

            _crackRoot = new GameObject("CrackRoot").transform;
            _crackRoot.SetParent(_root.transform, false);
            _crackRoot.localPosition = Vector3.zero;
            _crackRoot.localRotation = Quaternion.identity;
            _crackRenderers = CreateCrackVisuals(isPreview);
            _branchCrackRenderers = CreateBranchCrackVisuals(isPreview);
            _chipRenderers = CreateChipVisuals(isPreview);

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
            _barFill.localPosition = new Vector3(0f, 0f, 0.016f);
            _barFill.localScale = new Vector3(Mathf.Max(0.4f, _size.x * 0.74f), 0.04f, 0.02f);
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
            // 血量变化同时驱动红色血条长度和墙面裂痕强度。
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
            _barFill.localPosition = new Vector3(-(backgroundWidth - fillWidth) * 0.5f, 0f, 0.016f);
            UpdateCrackVisuals(1f - normalized);
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

        private Renderer[] CreateCrackVisuals(bool isPreview)
        {
            var renderers = new Renderer[CrackCountPerFace * 2];
            if (isPreview)
            {
                if (_crackRoot != null)
                {
                    _crackRoot.gameObject.SetActive(false);
                }

                return renderers;
            }

            var frontZ = _size.z * 0.5f + 0.006f;
            var crackColor = new Color(0.05f, 0.02f, 0.02f, 0f);
            for (var i = 0; i < CrackCountPerFace; i++)
            {
                renderers[i] = CreateSingleCrack($"CrackFront_{i}", GetCrackLocalPosition(i, frontZ), GetCrackLocalRotation(i, false), crackColor);
                renderers[i + CrackCountPerFace] = CreateSingleCrack($"CrackBack_{i}", GetCrackLocalPosition(i, -frontZ), GetCrackLocalRotation(i, true), crackColor);
            }

            return renderers;
        }

        private Renderer[] CreateBranchCrackVisuals(bool isPreview)
        {
            var renderers = new Renderer[BranchCrackCountPerFace * 2];
            if (isPreview)
            {
                return renderers;
            }

            var frontZ = _size.z * 0.5f + 0.007f;
            var crackColor = new Color(0.06f, 0.03f, 0.03f, 0f);
            for (var i = 0; i < BranchCrackCountPerFace; i++)
            {
                renderers[i] = CreateBranchCrack($"BranchFront_{i}", GetBranchCrackLocalPosition(i, frontZ), GetBranchCrackLocalRotation(i, false), crackColor);
                renderers[i + BranchCrackCountPerFace] = CreateBranchCrack($"BranchBack_{i}", GetBranchCrackLocalPosition(i, -frontZ), GetBranchCrackLocalRotation(i, true), crackColor);
            }

            return renderers;
        }

        private Renderer[] CreateChipVisuals(bool isPreview)
        {
            var renderers = new Renderer[ChipCountPerFace * 2];
            if (isPreview)
            {
                return renderers;
            }

            var frontZ = _size.z * 0.5f + 0.008f;
            var chipColor = new Color(0.9f, 0.92f, 0.96f, 0f);
            for (var i = 0; i < ChipCountPerFace; i++)
            {
                renderers[i] = CreateChip($"ChipFront_{i}", GetChipLocalPosition(i, frontZ), chipColor);
                renderers[i + ChipCountPerFace] = CreateChip($"ChipBack_{i}", GetChipLocalPosition(i, -frontZ), chipColor);
            }

            return renderers;
        }

        private Renderer CreateSingleCrack(string name, Vector3 localPosition, Quaternion localRotation, Color color)
        {
            var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack.name = name;
            crack.transform.SetParent(_crackRoot, false);
            crack.transform.localPosition = localPosition;
            crack.transform.localRotation = localRotation;
            crack.transform.localScale = new Vector3(_size.x * 0.16f, _size.y * 0.34f, 0.006f);

            var collider = crack.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = crack.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = BuildFlatMaterial(color, true);
            }

            return renderer;
        }

        private Renderer CreateBranchCrack(string name, Vector3 localPosition, Quaternion localRotation, Color color)
        {
            var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack.name = name;
            crack.transform.SetParent(_crackRoot, false);
            crack.transform.localPosition = localPosition;
            crack.transform.localRotation = localRotation;
            crack.transform.localScale = new Vector3(_size.x * 0.08f, _size.y * 0.18f, 0.005f);

            var collider = crack.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = crack.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = BuildFlatMaterial(color, true);
            }

            return renderer;
        }

        private Renderer CreateChip(string name, Vector3 localPosition, Color color)
        {
            var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = name;
            chip.transform.SetParent(_crackRoot, false);
            chip.transform.localPosition = localPosition;
            chip.transform.localRotation = Quaternion.identity;
            chip.transform.localScale = new Vector3(_size.x * 0.09f, _size.y * 0.1f, 0.007f);

            var collider = chip.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = chip.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = BuildFlatMaterial(color, true);
            }

            return renderer;
        }

        private Vector3 GetCrackLocalPosition(int index, float z)
        {
            var x = 0f;
            var y = 0f;
            switch (index)
            {
                case 0:
                    x = -_size.x * 0.24f;
                    y = _size.y * 0.12f;
                    break;
                case 1:
                    x = _size.x * 0.11f;
                    y = _size.y * 0.19f;
                    break;
                case 2:
                    x = -_size.x * 0.06f;
                    y = -_size.y * 0.07f;
                    break;
                default:
                    x = _size.x * 0.23f;
                    y = -_size.y * 0.16f;
                    break;
            }

            return new Vector3(x, y, z);
        }

        private Quaternion GetCrackLocalRotation(int index, bool backFace)
        {
            float zAngle;
            switch (index)
            {
                case 0:
                    zAngle = 26f;
                    break;
                case 1:
                    zAngle = -18f;
                    break;
                case 2:
                    zAngle = 11f;
                    break;
                default:
                    zAngle = -33f;
                    break;
            }

            return Quaternion.Euler(0f, backFace ? 180f : 0f, zAngle);
        }

        private Vector3 GetBranchCrackLocalPosition(int index, float z)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(-_size.x * 0.14f, _size.y * 0.03f, z);
                case 1:
                    return new Vector3(_size.x * 0.18f, _size.y * 0.11f, z);
                default:
                    return new Vector3(0.02f, -_size.y * 0.18f, z);
            }
        }

        private Quaternion GetBranchCrackLocalRotation(int index, bool backFace)
        {
            float zAngle;
            switch (index)
            {
                case 0:
                    zAngle = -48f;
                    break;
                case 1:
                    zAngle = 37f;
                    break;
                default:
                    zAngle = -22f;
                    break;
            }

            return Quaternion.Euler(0f, backFace ? 180f : 0f, zAngle);
        }

        private Vector3 GetChipLocalPosition(int index, float z)
        {
            return index == 0
                ? new Vector3(-_size.x * 0.32f, _size.y * 0.31f, z)
                : new Vector3(_size.x * 0.3f, -_size.y * 0.28f, z);
        }

        private void UpdateCrackVisuals(float damage01)
        {
            // 用受损比例驱动主裂痕、分叉裂纹和角落崩边的显示强度。
            if (_crackRenderers == null || _crackRenderers.Length == 0)
            {
                return;
            }

            var activePairs = Mathf.Clamp(Mathf.CeilToInt(damage01 * CrackCountPerFace), 0, CrackCountPerFace);
            var alpha = Mathf.Lerp(0f, 0.92f, damage01);
            var lengthScale = Mathf.Lerp(0.35f, 1.15f, damage01);
            for (var i = 0; i < _crackRenderers.Length; i++)
            {
                var renderer = _crackRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var crackIndex = i % CrackCountPerFace;
                var enabled = crackIndex < activePairs;
                renderer.enabled = enabled;
                if (!enabled)
                {
                    continue;
                }

                var transform = renderer.transform;
                var baseScale = transform.localScale;
                baseScale.y = _size.y * 0.34f * lengthScale * (1f + crackIndex * 0.08f);
                transform.localScale = baseScale;

                var color = renderer.material.color;
                color.a = alpha * (0.82f + crackIndex * 0.05f);
                renderer.material.color = color;
            }

            var branchThreshold = Mathf.InverseLerp(0.35f, 1f, damage01);
            var activeBranchPairs = Mathf.Clamp(Mathf.CeilToInt(branchThreshold * BranchCrackCountPerFace), 0, BranchCrackCountPerFace);
            for (var i = 0; i < _branchCrackRenderers.Length; i++)
            {
                var renderer = _branchCrackRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var crackIndex = i % BranchCrackCountPerFace;
                var enabled = crackIndex < activeBranchPairs;
                renderer.enabled = enabled;
                if (!enabled)
                {
                    continue;
                }

                var transform = renderer.transform;
                var baseScale = transform.localScale;
                baseScale.y = _size.y * 0.18f * Mathf.Lerp(0.65f, 1.35f, branchThreshold);
                transform.localScale = baseScale;

                var color = renderer.material.color;
                color.a = Mathf.Lerp(0f, 0.82f, branchThreshold) * (0.88f + crackIndex * 0.04f);
                renderer.material.color = color;
            }

            var chipThreshold = Mathf.InverseLerp(0.62f, 1f, damage01);
            var activeChipPairs = Mathf.Clamp(Mathf.CeilToInt(chipThreshold * ChipCountPerFace), 0, ChipCountPerFace);
            for (var i = 0; i < _chipRenderers.Length; i++)
            {
                var renderer = _chipRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var chipIndex = i % ChipCountPerFace;
                var enabled = chipIndex < activeChipPairs;
                renderer.enabled = enabled;
                if (!enabled)
                {
                    continue;
                }

                var transform = renderer.transform;
                var baseScale = transform.localScale;
                var scale = Mathf.Lerp(0.45f, 1.2f, chipThreshold);
                baseScale.x = _size.x * 0.09f * scale;
                baseScale.y = _size.y * 0.1f * scale;
                transform.localScale = baseScale;

                var color = renderer.material.color;
                color.a = Mathf.Lerp(0f, 0.9f, chipThreshold);
                renderer.material.color = color;
            }
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



