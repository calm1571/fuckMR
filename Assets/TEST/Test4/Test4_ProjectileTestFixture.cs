using System.Collections;
using NUnit.Framework;
using Project.Gameplay.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public abstract class Test4_ProjectileTestFixture
    {
        protected const float PositionTolerance = 0.02f;
        protected const float DirectionTolerance = 0.001f;

        protected GameObject root;
        protected M1ProjectileShooter shooter;
        protected Transform shootOrigin;
        protected int shotEventCount;
        protected M1ProjectileShooter.ShotInfo lastShotInfo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject($"{GetType().Name}_Root");
            shooter = root.AddComponent<M1ProjectileShooter>();

            var originGo = new GameObject("ShootOrigin");
            originGo.transform.SetParent(root.transform, false);
            originGo.transform.localPosition = Vector3.zero;
            originGo.transform.localRotation = Quaternion.identity;
            shootOrigin = originGo.transform;

            shooter.SetShootOrigin(shootOrigin);
            shooter.SetCombatTuning(speed: 6f, radius: 0.05f, cooldown: 0.05f);
            shooter.SetShootingEnabled(true);
            shooter.ShotFired += OnShotFired;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (shooter != null)
            {
                shooter.ShotFired -= OnShotFired;
            }

            foreach (var projectile in Object.FindObjectsOfType<M1Projectile>())
            {
                if (projectile != null)
                {
                    Object.Destroy(projectile.gameObject);
                }
            }

            foreach (var tag in Object.FindObjectsOfType<WallObstacleColliderTag>())
            {
                if (tag != null)
                {
                    Object.Destroy(tag.gameObject);
                }
            }

            if (root != null)
            {
                Object.Destroy(root);
            }

            yield return null;
        }

        protected M1Projectile SpawnLocalProjectile(Vector3 direction)
        {
            lastShotInfo = default;
            var before = Object.FindObjectsOfType<M1Projectile>();
            shootOrigin.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            SendTriggerDown(shooter);
            return FindNewProjectile(before);
        }

        protected M1Projectile SpawnRemoteProjectile(Vector3 spawnPos, Vector3 direction, float speed = 6f, float maxDistance = 25f, float lifetime = 6f)
        {
            var before = Object.FindObjectsOfType<M1Projectile>();
            shooter.SpawnRemoteProjectile(spawnPos, direction, speed, maxDistance, lifetime);
            return FindNewProjectile(before);
        }

        protected static int CountProjectiles()
        {
            return Object.FindObjectsOfType<M1Projectile>().Length;
        }

        protected static IEnumerator WaitFixedFrames(int frameCount)
        {
            for (var i = 0; i < frameCount; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        protected static GameObject CreateWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestWallObstacle";
            wall.transform.position = position;
            wall.transform.localScale = scale;
            if (wall.GetComponent<WallObstacleColliderTag>() == null)
            {
                wall.AddComponent<WallObstacleColliderTag>();
            }

            var rb = wall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            return wall;
        }

        private void OnShotFired(M1ProjectileShooter.ShotInfo info)
        {
            shotEventCount++;
            lastShotInfo = info;
        }

        private static void SendTriggerDown(M1ProjectileShooter target)
        {
            var method = typeof(M1ProjectileShooter).GetMethod("OnTriggerDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method, "Failed to find M1ProjectileShooter.OnTriggerDown via reflection.");
            method.Invoke(target, null);
        }

        private static M1Projectile FindNewProjectile(M1Projectile[] before)
        {
            var after = Object.FindObjectsOfType<M1Projectile>();
            for (var i = 0; i < after.Length; i++)
            {
                var candidate = after[i];
                var existed = false;
                for (var j = 0; j < before.Length; j++)
                {
                    if (before[j] == candidate)
                    {
                        existed = true;
                        break;
                    }
                }

                if (!existed)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
