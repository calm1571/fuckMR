using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Project.Core
{
        /// <summary>
    /// Spectator 运行时控制面板。
    /// </summary>
    public sealed class SpectatorControlView
    {
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly Vector3 _localOffset;
        private readonly TMP_Text _statusText;
        private readonly Button _healHostButton;
        private readonly Button _healClientButton;
        private readonly Button _barrageAButton;
        private readonly Button _barrageBButton;
        private readonly Button _barrageCButton;
        private readonly Button _cheerButton;
        private readonly Button _applauseButton;
        private readonly Button _placeWallButton;
        private readonly TMP_Text _barrageALabel;
        private readonly TMP_Text _barrageBLabel;
        private readonly TMP_Text _barrageCLabel;

        public SpectatorControlView(
            Transform cameraTransform,
            float distance,
            Vector3 localOffset,
            Action onHealHost,
            Action onHealClient,
            Action onBarrageA,
            Action onBarrageB,
            Action onBarrageC,
            Action onCheer,
            Action onApplause,
            Action onPlaceWall)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(0.8f, distance);
            _localOffset = localOffset;
            _root = new GameObject("SpectatorControlRoot");

            EnsureEventSystem();
            (_statusText, _healHostButton, _healClientButton, _barrageAButton, _barrageBButton, _barrageCButton, _cheerButton, _applauseButton, _placeWallButton, _barrageALabel, _barrageBLabel, _barrageCLabel) =
                BuildCanvas(onHealHost, onHealClient, onBarrageA, onBarrageB, onBarrageC, onCheer, onApplause, onPlaceWall);
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

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void SetButtonsInteractable(bool canHealHost, bool canHealClient, bool canBarrage, bool canAudio, bool canPlaceWall)
        {
            if (_healHostButton != null)
            {
                _healHostButton.interactable = canHealHost;
            }

            if (_healClientButton != null)
            {
                _healClientButton.interactable = canHealClient;
            }

            if (_barrageAButton != null)
            {
                _barrageAButton.interactable = canBarrage;
            }

            if (_barrageBButton != null)
            {
                _barrageBButton.interactable = canBarrage;
            }

            if (_barrageCButton != null)
            {
                _barrageCButton.interactable = canBarrage;
            }

            if (_cheerButton != null)
            {
                _cheerButton.interactable = canAudio;
            }

            if (_applauseButton != null)
            {
                _applauseButton.interactable = canAudio;
            }

            if (_placeWallButton != null)
            {
                _placeWallButton.interactable = canPlaceWall;
            }
        }

        public void SetBarrageLabels(string labelA, string labelB, string labelC)
        {
            if (_barrageALabel != null)
            {
                _barrageALabel.text = string.IsNullOrWhiteSpace(labelA) ? "COOL" : labelA;
            }

            if (_barrageBLabel != null)
            {
                _barrageBLabel.text = string.IsNullOrWhiteSpace(labelB) ? "GOOD GAME" : labelB;
            }

            if (_barrageCLabel != null)
            {
                _barrageCLabel.text = string.IsNullOrWhiteSpace(labelC) ? "NICE SHOT" : labelC;
            }
        }

        private (TMP_Text status, Button healHost, Button healClient, Button barrageA, Button barrageB, Button barrageC, Button cheer, Button applause, Button placeWall, TMP_Text barrageALabel, TMP_Text barrageBLabel, TMP_Text barrageCLabel)
            BuildCanvas(Action onHealHost, Action onHealClient, Action onBarrageA, Action onBarrageB, Action onBarrageC, Action onCheer, Action onApplause, Action onPlaceWall)
        {
            var canvasGo = new GameObject(
                "SpectatorControlCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 121;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(980f, 830f);
            canvasRect.localScale = Vector3.one * 0.0011f;

            var panel = CreateImage("Panel", canvasRect, Vector2.zero, new Vector2(940f, 790f), new Color(0f, 0f, 0f, 0.62f));
            var title = CreateText("Title", panel, new Vector2(0f, 334f), new Vector2(860f, 70f), 42f, TextAlignmentOptions.Center);
            title.text = "Spectator Support";

            var status = CreateText("Status", panel, new Vector2(0f, 264f), new Vector2(860f, 90f), 30f, TextAlignmentOptions.Center);
            status.text = "Vote to heal a player";

            var healHost = CreateButton(panel, "Heal Host", new Vector2(-190f, 174f), onHealHost, out _);
            var healClient = CreateButton(panel, "Heal Client", new Vector2(190f, 174f), onHealClient, out _);

            var wallCaption = CreateText("WallCaption", panel, new Vector2(0f, 76f), new Vector2(860f, 54f), 30f, TextAlignmentOptions.Center);
            wallCaption.text = "Obstacle";

            var placeWall = CreateButton(panel, "Place Wall", new Vector2(0f, -6f), onPlaceWall, out _);

            var audioCaption = CreateText("AudioCaption", panel, new Vector2(0f, -104f), new Vector2(860f, 54f), 30f, TextAlignmentOptions.Center);
            audioCaption.text = "Local Audio";

            var cheer = CreateButton(panel, "Cheer", new Vector2(-190f, -184f), onCheer, out _);
            var applause = CreateButton(panel, "Applause", new Vector2(190f, -184f), onApplause, out _);

            var barrageCaption = CreateText("BarrageCaption", panel, new Vector2(0f, -294f), new Vector2(860f, 54f), 30f, TextAlignmentOptions.Center);
            barrageCaption.text = "Quick Barrage";

            var barrageA = CreateButton(panel, "COOL", new Vector2(-250f, -384f), onBarrageA, out var barrageALabel);
            var barrageB = CreateButton(panel, "GOOD GAME", new Vector2(0f, -384f), onBarrageB, out var barrageBLabel);
            var barrageC = CreateButton(panel, "NICE SHOT", new Vector2(250f, -384f), onBarrageC, out var barrageCLabel);

            return (status, healHost, healClient, barrageA, barrageB, barrageC, cheer, applause, placeWall, barrageALabel, barrageBLabel, barrageCLabel);
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

            text.fontSize = sizePx;
            text.color = Color.white;
            text.alignment = align;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = sizePx;
            return text;
        }

        private static Button CreateButton(RectTransform parent, string text, Vector2 anchoredPos, Action onClick, out TMP_Text label)
        {
            var buttonRect = CreateImage(text + "Button", parent, anchoredPos, new Vector2(220f, 96f), new Color(0.16f, 0.22f, 0.28f, 1f));
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

            label = CreateText("Label", buttonRect, Vector2.zero, new Vector2(190f, 74f), 30f, TextAlignmentOptions.Center);
            label.text = text;
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

