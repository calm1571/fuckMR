using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Core
{
        /// <summary>
    /// Spectator 本地弹幕视图。
    /// </summary>
    public sealed class SpectatorBarrageView
    {
                /// <summary>
        /// 单条弹幕实例记录。
        /// </summary>
        private sealed class Entry
        {
            public RectTransform rect;
            public TextMeshProUGUI text;
            public float lifetime;
            public float duration;
            public float speed;
            public float startX;
            public float startY;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly GameObject _root;
        private readonly Transform _cameraTransform;
        private readonly float _distance;
        private readonly Vector3 _localOffset;
        private readonly RectTransform _canvasRect;

        public SpectatorBarrageView(Transform cameraTransform, float distance, Vector3 localOffset)
        {
            _cameraTransform = cameraTransform;
            _distance = Mathf.Max(0.6f, distance);
            _localOffset = localOffset;
            _root = new GameObject("SpectatorBarrageRoot");
            _canvasRect = BuildCanvas();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void Tick(float deltaTime)
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

            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.lifetime += deltaTime;
                var normalized = Mathf.Clamp01(entry.lifetime / Mathf.Max(0.01f, entry.duration));
                var x = Mathf.Lerp(entry.startX, -640f, normalized * entry.speed);
                var y = entry.startY + Mathf.Sin(normalized * Mathf.PI) * 16f;
                entry.rect.anchoredPosition = new Vector2(x, y);

                var color = entry.text.color;
                color.a = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.65f, 1f, normalized));
                entry.text.color = color;

                if (entry.lifetime >= entry.duration)
                {
                    Object.Destroy(entry.rect.gameObject);
                    _entries.RemoveAt(i);
                }
            }
        }

        public void ShowMessage(string message, float duration, float speed)
        {
            if (_canvasRect == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var go = new GameObject("BarrageItem", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_canvasRect, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 80f);

            var text = go.GetComponent<TextMeshProUGUI>();
            var font = GetSafeFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = message;
            text.fontSize = 42f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 42f;
            text.color = new Color(1f, 1f, 1f, 1f);
            text.alignment = TextAlignmentOptions.Center;

            var entry = new Entry
            {
                rect = rect,
                text = text,
                lifetime = 0f,
                duration = Mathf.Max(0.8f, duration),
                speed = Mathf.Max(0.15f, speed),
                startX = 520f + Random.Range(0f, 80f),
                startY = Random.Range(-90f, 120f)
            };
            rect.anchoredPosition = new Vector2(entry.startX, entry.startY);
            _entries.Add(entry);
        }

        private RectTransform BuildCanvas()
        {
            var canvasGo = new GameObject(
                "SpectatorBarrageCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasGo.transform.SetParent(_root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 140;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 12f;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280f, 720f);
            rect.localScale = Vector3.one * 0.00105f;
            return rect;
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

