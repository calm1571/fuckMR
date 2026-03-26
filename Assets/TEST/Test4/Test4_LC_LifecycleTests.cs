using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class Test4_LC_LifecycleTests : Test4_ProjectileTestFixture
    {
        [UnityTest]
        public IEnumerator LC_01_Projectile_Is_Destroyed_On_Lifetime_Timeout()
        {
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 1f, maxDistance: 100f, lifetime: 0.1f);
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(projectile == null);
            Assert.AreEqual(0, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator LC_02_Projectile_Is_Destroyed_On_Max_Distance()
        {
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 5f, maxDistance: 0.4f, lifetime: 5f);
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(projectile == null);
            Assert.AreEqual(0, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator LC_05_Wall_Collision_Destroys_Local_Projectile_Immediately()
        {
            CreateWall(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 1f, 0.1f));
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 8f, maxDistance: 10f, lifetime: 5f);

            yield return WaitFixedFrames(10);

            Assert.IsTrue(projectile == null);
            Assert.AreEqual(0, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator LC_06_Wall_Collision_Near_Lifetime_Still_Uses_Single_Destroy_Path()
        {
            CreateWall(new Vector3(0f, 0f, 0.2f), new Vector3(1f, 1f, 0.1f));
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 6f, maxDistance: 10f, lifetime: 0.12f);

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(projectile == null);
            Assert.AreEqual(0, CountProjectiles());
        }
    }
}
