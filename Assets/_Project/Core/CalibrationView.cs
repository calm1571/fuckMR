// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Builds and updates the 3D calibration user interface.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Project.Core
{
        /// <summary>
    /// World-space UI used during calibration.
    /// </summary>
    public sealed class CalibrationView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly float _verticalOffset;
        private readonly TMP_Text _statusText;
        private readonly TMP_Text _detectionText;
        private readonly Button _confirmButton;
        private readonly Button _backButton;
        private readonly TMP_Text _confirmLabel;

        public CalibrationView(Transform cameraTransform, Action onConfirm, Action onBack, float distance, float verticalOffset)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(1.8f, distance);
            _verticalOffset = verticalOffset;
            _root = new GameObject("CalibrationRoot");

            EnsureEventSystem();
            BuildCanvas(onConfirm, onBack, out _statusText, out _detectionText, out _confirmButton, out _backButton, out _confirmLabel);
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void SetDetectionStatus(string status)
        {
            if (_detectionText != null)
            {
                _detectionText.text = status;
            }
        }

        public void SetConfirmVisible(bool visible)
        {
            if (_confirmButton != null)
            {
                _confirmButton.gameObject.SetActive(visible);
            }

            if (_backButton != null)
            {
                var rect = _backButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = visible ? new Vector2(0f, -350f) : new Vector2(0f, -210f);
                }
            }
        }

        public void SetConfirmText(string text)
        {
            if (_confirmLabel != null && !string.IsNullOrEmpty(text))
            {
                _confirmLabel.text = text;
            }
        }

        public void Tick()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            var forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            _root.transform.position = _cameraTransform.position + forward * _distance + Vector3.up * _verticalOffset;
            _root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void BuildCanvas(Action onConfirm, Action onBack, out TMP_Text statusText, out TMP_Text detectionText, out Button confirmButton, out Button backButton, out TMP_Text confirmLabel)
        {
            var canvasGo = new GameObject(
                "CalibrationCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 55;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1024f, 820f);
            canvasRect.localScale = Vector3.one * 0.0012f;

            var panel = CreateImage("Panel", canvasRect, Vector2.zero, new Vector2(980f, 760f), new Color(0.06f, 0.11f, 0.15f, 0.78f));
            CreateTitle(panel, "Calibration");
            statusText = CreateBody(panel, "Right Stick: Move XZ\nLeft Stick: Rotate Y\nA/B: Height +/-");
            detectionText = CreateDetectionBlock(panel, "<color=#6CA9D9>Detection: Initializing...</color>");
            confirmButton = CreateButton(panel, "Confirm", new Vector2(0f, -210f), onConfirm, out confirmLabel);
            backButton = CreateButton(panel, "Back", new Vector2(0f, -350f), onBack);
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

        private static void CreateTitle(RectTransform parent, string text)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            rect.sizeDelta = new Vector2(880f, 120f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 74f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static TMP_Text CreateBody(RectTransform parent, string text)
        {
            var go = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 135f);
            rect.sizeDelta = new Vector2(880f, 220f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 34f;
            label.enableAutoSizing = false;
            label.color = new Color(0.9f, 0.95f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static TMP_Text CreateDetectionBlock(RectTransform parent, string text)
        {
            var block = CreateImage("DetectionBlock", parent, new Vector2(0f, -50f), new Vector2(880f, 180f), new Color(0.03f, 0.06f, 0.09f, 0.92f));

            var go = new GameObject("DetectionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(block, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 18f);
            rect.offsetMax = new Vector2(-24f, -18f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 32f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 32f;
            label.color = new Color(0.85f, 0.93f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            return label;
        }

        private static Button CreateButton(RectTransform parent, string text, Vector2 anchoredPos, Action onClick)
        {
            TMP_Text _;
            return CreateButton(parent, text, anchoredPos, onClick, out _);
        }

        private static Button CreateButton(RectTransform parent, string text, Vector2 anchoredPos, Action onClick, out TMP_Text labelOut)
        {
            var buttonRect = CreateImage(text + "Button", parent, anchoredPos, new Vector2(700f, 118f), new Color(0.14f, 0.25f, 0.34f, 1f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.24f, 0.33f, 1f);
            colors.highlightedColor = new Color(0.22f, 0.43f, 0.56f, 1f);
            colors.pressedColor = new Color(0.09f, 0.31f, 0.44f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.targetGraphic = buttonRect.GetComponent<Image>();
            button.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonRect, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 8f);
            textRect.offsetMax = new Vector2(-24f, -8f);

            var label = textGo.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 52f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 34f;
            label.fontSizeMax = 52f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            labelOut = label;
            return button;
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

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                go.GetComponent<EventSystem>().sendNavigationEvents = false;
                return;
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }

            eventSystem.sendNavigationEvents = false;
        }
    }
}




