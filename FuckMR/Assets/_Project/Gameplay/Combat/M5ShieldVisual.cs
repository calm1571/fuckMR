using UnityEngine;

namespace Project.Gameplay.Combat
{
    public sealed class M5ShieldVisual : MonoBehaviour
    {
        [SerializeField] private Color shieldColor = new Color(0.2f, 0.9f, 1f, 0.35f);
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.1f, 0f);
        [SerializeField] private Vector3 localScale = new Vector3(0.55f, 0.02f, 0.55f);

        private Transform _anchor;
        private GameObject _visual;
        private float _activeUntil;

        public bool IsActive => _visual != null && _visual.activeSelf;
        public float RemainingSeconds => Mathf.Max(0f, _activeUntil - Time.time);

        public void BindAnchor(Transform anchor)
        {
            _anchor = anchor;
            EnsureVisual();
            UpdateTransform();
        }

        public void Activate(float durationSeconds)
        {
            EnsureVisual();
            _activeUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
            _visual.SetActive(true);
        }

        public void Deactivate()
        {
            _activeUntil = 0f;
            if (_visual != null)
            {
                _visual.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_visual == null)
            {
                return;
            }

            UpdateTransform();
            if (_visual.activeSelf && Time.time >= _activeUntil)
            {
                _visual.SetActive(false);
            }
        }

        private void EnsureVisual()
        {
            if (_visual != null)
            {
                return;
            }

            _visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _visual.name = "ShieldVisual";
            _visual.transform.SetParent(transform, false);
            _visual.transform.localScale = localScale;
            _visual.SetActive(false);

            var col = _visual.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            var renderer = _visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = shieldColor;
                    renderer.material = mat;
                }
            }
        }

        private void UpdateTransform()
        {
            if (_anchor == null || _visual == null)
            {
                return;
            }

            _visual.transform.position = _anchor.position + _anchor.TransformVector(localOffset);
            var forward = Vector3.ProjectOnPlane(_anchor.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            _visual.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
