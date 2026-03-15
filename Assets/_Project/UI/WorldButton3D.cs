using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Project.UI
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class WorldButton3D : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _normalColor;
        private Color _hoverColor;
        private Action _onClick;
        private XRSimpleInteractable _interactable;
        private bool _eventsBound;

        private void Awake()
        {
            EnsureInteractor();
            BindEvents();
        }

        private void OnDestroy()
        {
            if (!_eventsBound || _interactable == null)
            {
                return;
            }

            _interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _interactable.hoverExited.RemoveListener(OnHoverExited);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _eventsBound = false;
        }

        public void Initialize(string label, Action onClick, Color normalColor, Color hoverColor)
        {
            _renderer = GetComponent<Renderer>();
            _normalColor = normalColor;
            _hoverColor = hoverColor;
            _onClick = onClick;
            EnsureInteractor();
            BindEvents();
            SetHighlighted(false);
            SetupLabel(label);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.material.color = highlighted ? _hoverColor : _normalColor;
        }

        private void EnsureInteractor()
        {
            if (_interactable != null)
            {
                return;
            }

            _interactable = GetComponent<XRSimpleInteractable>();
            if (_interactable == null)
            {
                _interactable = gameObject.AddComponent<XRSimpleInteractable>();
            }
        }

        private void BindEvents()
        {
            if (_eventsBound || _interactable == null)
            {
                return;
            }

            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.hoverExited.AddListener(OnHoverExited);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _eventsBound = true;
        }

        private void OnHoverEntered(HoverEnterEventArgs _)
        {
            SetHighlighted(true);
        }

        private void OnHoverExited(HoverExitEventArgs _)
        {
            SetHighlighted(false);
        }

        private void OnSelectEntered(SelectEnterEventArgs _)
        {
            Click();
        }

        public void Click()
        {
            _onClick?.Invoke();
        }

        private void SetupLabel(string label)
        {
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.031f);
            textGo.transform.localRotation = Quaternion.identity;
            textGo.transform.localScale = Vector3.one;

            var mesh = textGo.AddComponent<TextMesh>();
            mesh.text = label;
            mesh.fontSize = 64;
            var availableWidth = Mathf.Max(0.2f, transform.localScale.x * 0.78f);
            mesh.characterSize = Mathf.Clamp((availableWidth / Mathf.Max(4, label.Length)) * 0.95f, 0.04f, 0.06f);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
        }
    }
}
