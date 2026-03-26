using System.Collections;
using NUnit.Framework;
using Project.Gameplay.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class Test4_MV_MotionTests : Test4_ProjectileTestFixture
    {
        [UnityTest]
        public IEnumerator MV_01_Projectile_Moves_Forward_Stably()
        {
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 5f, maxDistance: 10f, lifetime: 5f);
            var start = projectile.transform.position;

            yield return WaitFixedFrames(10);

            Assert.That(projectile.transform.position.z, Is.GreaterThan(start.z + 0.5f));
            Assert.That(Mathf.Abs(projectile.transform.position.x - start.x), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator MV_02_Projectile_Speed_Tracks_Configured_Value()
        {
            const float speed = 5f;
            var projectile = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: speed, maxDistance: 10f, lifetime: 5f);
            var start = projectile.transform.position;

            yield return WaitFixedFrames(10);

            var elapsed = Time.fixedDeltaTime * 10f;
            var traveled = Vector3.Distance(start, projectile.transform.position);
            var measuredSpeed = traveled / elapsed;
            Assert.That(Mathf.Abs(measuredSpeed - speed), Is.LessThan(speed * 0.2f));
        }

        [UnityTest]
        public IEnumerator MV_03_Projectile_Does_Not_Follow_Later_ShootOrigin_Movement()
        {
            var projectile = SpawnLocalProjectile(Vector3.forward);
            yield return null;

            var projectileStart = projectile.transform.position;
            shootOrigin.position = new Vector3(3f, 2f, 3f);

            yield return WaitFixedFrames(5);

            Assert.That(projectile.transform.position.z, Is.GreaterThan(projectileStart.z));
            Assert.That(Vector3.Distance(projectile.transform.position, shootOrigin.position), Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator MV_04_Multiple_Projectiles_Move_Independently()
        {
            var projectileA = SpawnRemoteProjectile(Vector3.zero, Vector3.forward, speed: 5f, maxDistance: 10f, lifetime: 5f);
            var projectileB = SpawnRemoteProjectile(new Vector3(1f, 0f, 0f), Vector3.right, speed: 6f, maxDistance: 10f, lifetime: 5f);

            yield return WaitFixedFrames(8);

            Assert.NotNull(projectileA);
            Assert.NotNull(projectileB);
            Assert.That(projectileA.transform.position.z, Is.GreaterThan(0.3f));
            Assert.That(projectileB.transform.position.x, Is.GreaterThan(1.3f));
        }
    }
}
