using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class Test4_PJ_SpawnQuantityTests : Test4_ProjectileTestFixture
    {
        [UnityTest]
        public IEnumerator PJ_01_One_Cast_Spawns_One_Projectile()
        {
            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator PJ_02_Cooldown_Rejected_Cast_Does_Not_Spawn_Extra_Projectile()
        {
            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator PJ_03_Disabled_Shooting_Does_Not_Spawn_Projectile()
        {
            shooter.SetShootingEnabled(false);
            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            Assert.AreEqual(0, shotEventCount);
            Assert.AreEqual(0, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator PJ_04_High_Frequency_Legal_Casting_Matches_Successful_Count()
        {
            const int casts = 8;
            for (var i = 0; i < casts; i++)
            {
                SpawnLocalProjectile(Vector3.forward);
                yield return new WaitForSeconds(0.06f);
            }

            Assert.AreEqual(casts, shotEventCount);
            Assert.AreEqual(casts, CountProjectiles());
        }
    }
}
