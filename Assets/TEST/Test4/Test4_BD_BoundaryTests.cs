using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class Test4_BD_BoundaryTests : Test4_ProjectileTestFixture
    {
        [UnityTest]
        public IEnumerator BD_02_Large_Pitch_Fire_Preserves_Forward_Direction()
        {
            var direction = new Vector3(0f, 0.95f, 0.31f).normalized;
            SpawnLocalProjectile(direction);
            yield return null;

            Assert.That(Vector3.Distance(direction, lastShotInfo.direction), Is.LessThan(DirectionTolerance));
        }

        [UnityTest]
        public IEnumerator BD_06_Fire_Directly_Into_Nearby_Wall_Destroys_Projectile_Without_Tunneling()
        {
            shootOrigin.position = new Vector3(0f, 0f, 0f);
            CreateWall(new Vector3(0f, 0f, 0.18f), new Vector3(0.6f, 0.6f, 0.08f));
            var projectile = SpawnRemoteProjectile(new Vector3(0f, 0f, 0.08f), Vector3.forward, speed: 5f, maxDistance: 5f, lifetime: 5f);

            yield return WaitFixedFrames(8);

            Assert.IsTrue(projectile == null);
            Assert.AreEqual(0, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator BD_07_Repeated_Fire_Into_Walls_Does_Not_Leave_Lingering_Projectiles()
        {
            CreateWall(new Vector3(0f, 0f, 0.45f), new Vector3(1f, 1f, 0.1f));

            for (var i = 0; i < 5; i++)
            {
                SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 8f, maxDistance: 10f, lifetime: 5f);
                yield return WaitFixedFrames(6);
            }

            yield return null;
            Assert.AreEqual(0, CountProjectiles());
        }
    }
}
