using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Project.Core
{
        /// <summary>
    /// 通用 Lobby 世界空间 UI 视图。
    /// </summary>
    public sealed class LobbyView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly float _verticalOffset;
        private readonly TMP_Text _statusText;
        private readonly Button _primaryButton;
        private readonly TMP_Text _primaryLabel;
        private readonly TMP_InputField _inputField;
        private readonly TMP_Text _inputCaption;

        public LobbyView(Transform cameraTransform, string title, string primaryLabel, Action onPrimary, Action onBack, float distance, float verticalOffset)
            : this(cameraTransform, title, primaryLabel, onPrimary, onBack, distance, verticalOffset, null, string.Empty)
        {
        }

        public LobbyView(Transform cameraTransform, string title, string primaryLabel, Action onPrimary, Action onBack, float distance, float verticalOffset, Action<string> onInputChanged, string inputCaption)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(1.8f, distance);
            _verticalOffset = verticalOffset;
            _root = new GameObject(title.Replace(" ", string.Empty) + "Root");

            EnsureEventSystem();
            BuildCanvas(title, primaryLabel, onPrimary, onBack, onInputChanged, inputCaption, out _statusText, out _primaryButton, out _primaryLabel, out _inputField, out _inputCaption);
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

        public void SetInputVisible(bool visible)
        {
            if (_inputField != null)
            {
                _inputField.gameObject.SetActive(visible);
            }

            if (_inputCaption != null)
            {
                _inputCaption.gameObject.SetActive(visible);
            }
        }

        public void SetInputValue(string value)
        {
            if (_inputField != null)
            {
                _inputField.text = value ?? string.Empty;
            }
        }

        public string GetInputValue()
        {
            return _inputField != null ? _inputField.text : string.Empty;
        }

        private void BuildCanvas(string title, string primaryLabel, Action onPrimary, Action onBack, Action<string> onInputChanged, string inputCaptionText, out TMP_Text status, out Button primaryButton, out TMP_Text primaryLabelText, out TMP_InputField inputField, out TMP_Text inputCaption)
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
            inputCaption = null;
            inputField = null;
            if (onInputChanged != null)
            {
                inputCaption = CreateSmallLabel(panel, string.IsNullOrWhiteSpace(inputCaptionText) ? "Host IP" : inputCaptionText, new Vector2(0f, -60f));
                inputField = CreateInputField(panel, new Vector2(0f, -150f), onInputChanged);
            }

            var primaryY = onInputChanged != null ? -270f : -120f;
            var backY = onInputChanged != null ? -430f : -280f;
            var primary = CreateButton(panel, primaryLabel, new Vector2(0f, primaryY), onPrimary);
            primaryButton = primary.button;
            primaryLabelText = primary.label;
            CreateButton(panel, "Back", new Vector2(0f, backY), onBack);
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

        private static TMP_Text CreateSmallLabel(RectTransform parent, string text, Vector2 anchoredPos)
        {
            var go = new GameObject("InputCaption", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(760f, 54f);

            var label = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 30f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.78f, 0.88f, 0.96f, 1f);
            return label;
        }

        private static TMP_InputField CreateInputField(RectTransform parent, Vector2 anchoredPos, Action<string> onValueChanged)
        {
            var fieldRect = CreateImage("InputField", parent, anchoredPos, new Vector2(760f, 92f), new Color(0.05f, 0.08f, 0.11f, 0.92f));
            var input = fieldRect.gameObject.AddComponent<TMP_InputField>();

            var textViewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textViewport.transform.SetParent(fieldRect, false);
            var viewportRect = textViewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(28f, 12f);
            viewportRect.offsetMax = new Vector2(-28f, -12f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textViewport.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 40f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 26f;
            text.fontSizeMax = 40f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(textViewport.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                placeholder.font = font;
            }

            placeholder.text = "Enter Host IP";
            placeholder.fontSize = 36f;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.alignment = TextAlignmentOptions.Left;

            input.textViewport = viewportRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.onValueChanged.AddListener(value => onValueChanged?.Invoke(value));
            return input;
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

