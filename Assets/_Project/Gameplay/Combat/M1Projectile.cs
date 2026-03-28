// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Moves projectile visuals and handles local cleanup behaviour.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.Gameplay.Combat
{
        /// <summary>
    /// 子弹移动体，负责生命周期与本地墙碰撞销毁。
    /// </summary>
    public sealed class M1Projectile : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Vector3 _direction;
        private float _speed;
        private float _maxDistance;
        private float _lifetime;
        private float _aliveTime;
        private Rigidbody _rb;

        public void Initialize(Vector3 direction, float speed, float maxDistance, float lifetime)
        {
            _startPosition = transform.position;
            _direction = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            _speed = Mathf.Max(0.01f, speed);
            _maxDistance = Mathf.Max(0.1f, maxDistance);
            _lifetime = Mathf.Max(0.1f, lifetime);
            _aliveTime = 0f;

            EnsureRigidbody();
            if (_rb != null)
            {
                _rb.velocity = _direction * _speed;
            }
        }

        private void FixedUpdate()
        {
            if (_rb == null)
            {
                var step = _speed * Time.fixedDeltaTime;
                transform.position += _direction * step;
            }

            _aliveTime += Time.fixedDeltaTime;

            if (_aliveTime >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var traveled = Vector3.Distance(_startPosition, transform.position);
            if (traveled >= _maxDistance)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 视觉子弹打到墙时立即在本地销毁，避免出现穿墙显示。
            if (collision == null)
            {
                return;
            }

            if (IsWallObstacleCollision(collision.collider))
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsWallObstacleCollision(other))
            {
                Destroy(gameObject);
            }
        }

        private void EnsureRigidbody()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }

            _rb.useGravity = false;
            _rb.isKinematic = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private static bool IsWallObstacleCollision(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            return collider.GetComponent<WallObstacleColliderTag>() != null ||
                   collider.GetComponentInParent<WallObstacleColliderTag>() != null;
        }
    }
}



