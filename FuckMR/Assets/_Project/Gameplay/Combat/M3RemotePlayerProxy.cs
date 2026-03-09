using Project.Networking;
using UnityEngine;

namespace Project.Gameplay.Combat
{
    public sealed class M3RemotePlayerProxy : MonoBehaviour
    {
        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;
        private bool _initialized;

        private void Awake()
        {
            EnsureProxyObjects();
            SetVisible(false);
        }

        public void ApplyPose(PosePayload pose)
        {
            if (pose == null)
            {
                return;
            }

            EnsureProxyObjects();
            Apply(_head, pose.head);
            Apply(_leftHand, pose.leftHand);
            Apply(_rightHand, pose.rightHand);
            SetVisible(true);
        }

        private void EnsureProxyObjects()
        {
            if (_initialized)
            {
                return;
            }

            _head = CreatePart("RemoteHead", PrimitiveType.Sphere, new Vector3(0.20f, 0.20f, 0.20f), new Color(0.2f, 1f, 0.45f, 0.95f));
            _leftHand = CreatePart("RemoteLeftHand", PrimitiveType.Cube, new Vector3(0.08f, 0.08f, 0.08f), new Color(0.1f, 0.78f, 1f, 0.95f));
            _rightHand = CreatePart("RemoteRightHand", PrimitiveType.Cube, new Vector3(0.08f, 0.08f, 0.08f), new Color(0.1f, 0.78f, 1f, 0.95f));
            _initialized = true;
        }

        private Transform CreatePart(string name, PrimitiveType primitive, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = go.GetComponent<Renderer>();
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
                    mat.color = color;
                    renderer.material = mat;
                }
            }

            return go.transform;
        }

        private static void Apply(Transform target, PoseData pose)
        {
            if (target == null)
            {
                return;
            }

            target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void SetVisible(bool visible)
        {
            if (_head != null)
            {
                _head.gameObject.SetActive(visible);
            }

            if (_leftHand != null)
            {
                _leftHand.gameObject.SetActive(visible);
            }

            if (_rightHand != null)
            {
                _rightHand.gameObject.SetActive(visible);
            }
        }
    }
}
