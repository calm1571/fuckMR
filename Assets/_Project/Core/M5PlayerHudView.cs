using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Core
{
        /// <summary>
    /// 玩家本地 HUD，显示血量与冷却。
    /// </summary>
    public sealed class M5PlayerHudView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly Vector3 _localOffset;
        private readonly TMP_Text _hpText;
        private readonly TMP_Text _shootCdText;
        private readonly TMP_Text _shieldCdText;

        public M5PlayerHudView(Transform cameraTransform, float distance, Vector3 localOffset)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(0.8f, distance);
            _localOffset = localOffset;
            _root = new GameObject("PlayerHudRoot");
            (_hpText, _shootCdText, _shieldCdText) = BuildCanvas();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void Tick()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            var forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            _root.transform.position = _cameraTransform.position + forward * _distance + _cameraTransform.TransformVector(_localOffset);
            _root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public void SetStatus(int hp, int maxHp, float shootCooldownRemaining, float shieldCooldownRemaining)
        {
            if (_hpText != null)
            {
                _hpText.text = $"HP: {Mathf.Max(0, hp)}/{Mathf.Max(1, maxHp)}";
            }

            if (_shootCdText != null)
            {
                _shootCdText.text = $"Shoot CD: {Mathf.Max(0f, shootCooldownRemaining):F1}s";
            }

            if (_shieldCdText != null)
            {
                _shieldCdText.text = $"Shield CD: {Mathf.Max(0f, shieldCooldownRemaining):F1}s";
            }
        }

        private (TMP_Text hp, TMP_Text shootCd, TMP_Text shieldCd) BuildCanvas()
        {
            var canvasGo = new GameObject(
                "PlayerHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 120;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 200f);
            canvasRect.localScale = Vector3.one * 0.0011f;

            var panel = CreateImage(
                "Panel",
                canvasRect,
                Vector2.zero,
                new Vector2(960f, 180f),
                new Color(0f, 0f, 0f, 0.55f));

            var hp = CreateText("HpText", panel, new Vector2(-310f, 0f), new Vector2(300f, 120f), 48f, TextAlignmentOptions.Left);
            var shoot = CreateText("ShootCdText", panel, new Vector2(0f, 0f), new Vector2(300f, 120f), 44f, TextAlignmentOptions.Center);
            var shield = CreateText("ShieldCdText", panel, new Vector2(310f, 0f), new Vector2(300f, 120f), 44f, TextAlignmentOptions.Right);
            return (hp, shoot, shield);
        }

        private static RectTransform CreateImage(string name, RectTransform parent, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, Vector2 anchoredPos, Vector2 size, float sizePx, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = string.Empty;
            text.fontSize = sizePx;
            text.color = Color.white;
            text.alignment = align;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = sizePx;
            return text;
        }

        private static TMP_FontAsset GetSafeFontAsset()
        {
            TMP_FontAsset font = null;
            try
            {
                if (TMP_Settings.instance != null)
                {
                    font = TMP_Settings.defaultFontAsset;
                }
            }
            catch
            {
            }

            if (font != null)
            {
                return font;
            }

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }
}

