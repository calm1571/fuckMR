using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Project.Core
{
    public sealed class MainMenuView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly float _verticalOffset;

        public MainMenuView(Transform cameraTransform, Action onStart, Action onExit, float distance, float verticalOffset)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(1.6f, distance);
            _verticalOffset = verticalOffset;
            _root = new GameObject("MainMenuRoot");

            EnsureEventSystem();
            BuildCanvas(onStart, onExit);
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

            var anchor = _cameraTransform.position + (forward * _distance) + (Vector3.up * _verticalOffset);
            _root.transform.position = anchor;
            _root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void BuildCanvas(Action onStart, Action onExit)
        {
            var canvasGo = new GameObject(
                "MainMenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 760f);
            canvasRect.localScale = Vector3.one * 0.0012f;

            var panel = CreateImage("Panel", canvasRect, new Vector2(0f, 0f), new Vector2(940f, 700f), new Color(0.08f, 0.1f, 0.12f, 0.76f));
            CreateButton(panel, "Start Game", new Vector2(0f, 135f), onStart);
            CreateButton(panel, "Quit", new Vector2(0f, -135f), onExit);
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

            var image = go.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private static void CreateButton(RectTransform parent, string text, Vector2 anchoredPos, Action onClick)
        {
            var buttonRect = CreateImage(
                text + "Button",
                parent,
                anchoredPos,
                new Vector2(700f, 170f),
                new Color(0.16f, 0.22f, 0.28f, 1f));

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
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonRect, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 10f);
            textRect.offsetMax = new Vector2(-24f, -10f);

            var label = textGo.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();

            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = 72f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 42f;
            label.fontSizeMax = 72f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.overflowMode = TextOverflowModes.Ellipsis;
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
                // Ignore and fall back to Resources loading below.
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

            var xrUiInput = eventSystem.GetComponent<XRUIInputModule>();
            if (xrUiInput == null)
            {
                xrUiInput = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }

            eventSystem.sendNavigationEvents = false;
            xrUiInput.enabled = true;
        }
    }
}
