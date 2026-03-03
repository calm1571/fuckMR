using UnityEngine;

namespace Project.Gameplay.Combat
{
    public sealed class M1AlwaysVisibleControllerLaser : MonoBehaviour
    {
        [SerializeField] private float length = 8f;
        [SerializeField] private float width = 0.0045f;
        [SerializeField] private Color color = new Color(0.16f, 0.92f, 1f, 1f);
        [SerializeField] private Transform originOverride;

        private Transform _origin;
        private LineRenderer _line;

        private void Awake()
        {
            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
            }

            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = width;
            _line.endWidth = width * 0.8f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = color;
            _line.endColor = color;
            _line.enabled = true;
        }

        private void LateUpdate()
        {
            if (_origin == null)
            {
                _origin = originOverride != null ? originOverride : FindRightControllerTransform();
            }

            if (_origin == null)
            {
                return;
            }

            var start = _origin.position;
            var end = start + _origin.forward * length;
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
        }

        private static Transform FindRightControllerTransform()
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindByNameContains(roots[i].transform, "Right Controller");
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindByNameContains(Transform t, string token)
        {
            if (t.name.Contains(token))
            {
                return t;
            }

            for (var i = 0; i < t.childCount; i++)
            {
                var found = FindByNameContains(t.GetChild(i), token);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
