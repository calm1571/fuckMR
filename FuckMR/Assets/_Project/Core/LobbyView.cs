using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Project.Core
{
    public sealed class LobbyView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly float _verticalOffset;
        private readonly TMP_Text _statusText;
        private readonly Button _primaryButton;
        private readonly TMP_Text _primaryLabel;

        public LobbyView(Transform cameraTransform, string title, string primaryLabel, Action onPrimary, Action onBack, float distance, float verticalOffset)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(1.8f, distance);
            _verticalOffset = verticalOffset;
            _root = new GameObject(title.Replace(" ", string.Empty) + "Root");

            EnsureEventSystem();
            BuildCanvas(title, primaryLabel, onPrimary, onBack, out _statusText, out _primaryButton, out _primaryLabel);
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
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            _root.transform.position = _cameraTransform.position + forward * _distance + Vector3.up * _verticalOffset;
            _root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void SetPrimaryButton(string label, bool interactable)
        {
            if (_primaryLabel != null)
            {
                _primaryLabel.text = label;
            }

            if (_primaryButton != null)
            {
                _primaryButton.interactable = interactable;
            }
        }

        private void BuildCanvas(string title, string primaryLabel, Action onPrimary, Action onBack, out TMP_Text status, out Button primaryButton, out TMP_Text primaryLabelText)
        {
            var canvasGo = new GameObject(
                title.Replace(" ", string.Empty) + "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 53;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 780f);
            canvasRect.localScale = Vector3.one * 0.0012f;

            var panel = CreateImage("Panel", canvasRect, Vector2.zero, new Vector2(940f, 720f), new Color(0.08f, 0.11f, 0.15f, 0.78f));
            CreateTitle(panel, title);
            status = CreateBody(panel, "Waiting...");
            var primary = CreateButton(panel, primaryLabel, new Vector2(0f, -120f), onPrimary);
            primaryButton = primary.button;
            primaryLabelText = primary.label;
            CreateButton(panel, "Back", new Vector2(0f, -280f), onBack);
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
            rect.sizeDelta = new Vector2(860f, 120f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 68f;
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
            rect.anchoredPosition = new Vector2(0f, 100f);
            rect.sizeDelta = new Vector2(860f, 260f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 42f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 42f;
            label.color = new Color(0.9f, 0.95f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static (Button button, TMP_Text label) CreateButton(RectTransform parent, string text, Vector2 anchoredPos, Action onClick)
        {
            var buttonRect = CreateImage(text + "Button", parent, anchoredPos, new Vector2(700f, 130f), new Color(0.16f, 0.22f, 0.28f, 1f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.22f, 0.28f, 1f);
            colors.highlightedColor = new Color(0.22f, 0.42f, 0.56f, 1f);
            colors.pressedColor = new Color(0.12f, 0.33f, 0.46f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.65f);
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
            label.fontSize = 48f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 30f;
            label.fontSizeMax = 48f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return (button, label);
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
