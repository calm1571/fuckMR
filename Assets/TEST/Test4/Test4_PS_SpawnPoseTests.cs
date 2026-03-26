using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class Test4_PS_SpawnPoseTests : Test4_ProjectileTestFixture
    {
        [UnityTest]
        public IEnumerator PS_01_Spawn_Point_Is_In_Front_Of_Shoot_Origin()
        {
            shootOrigin.position = new Vector3(1f, 1.5f, 2f);
            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            var expected = shootOrigin.position + Vector3.forward * 0.08f;
            Assert.That(Vector3.Distance(lastShotInfo.spawnPosition, expected), Is.LessThan(PositionTolerance));
        }

        [UnityTest]
        public IEnumerator PS_02_Spawn_Point_Follows_Assigned_Shoot_Origin()
        {
            shootOrigin.position = new Vector3(0.3f, 1.1f, -0.7f);
            SpawnLocalProjectile(Vector3.right);
            yield return null;

            var expected = shootOrigin.position + Vector3.right * 0.08f;
            Assert.That(Vector3.Distance(lastShotInfo.spawnPosition, expected), Is.LessThan(PositionTolerance));
        }

        [UnityTest]
        public IEnumerator PS_03_Spawn_Direction_Matches_Aim_Direction()
        {
            var direction = new Vector3(1f, 0.2f, 0.4f).normalized;
            SpawnLocalProjectile(direction);
            yield return null;

            Assert.That(Vector3.Distance(direction, lastShotInfo.direction), Is.LessThan(DirectionTolerance));
        }

        [UnityTest]
        public IEnumerator PS_04_Close_Range_Still_Uses_Muzzle_Offset()
        {
            shootOrigin.position = new Vector3(0f, 1.2f, 0f);
            var wall = CreateWall(new Vector3(0f, 1.2f, 0.3f), new Vector3(0.4f, 0.4f, 0.1f));

            SpawnLocalProjectile(Vector3.forward);
            yield return null;

            var expected = shootOrigin.position + Vector3.forward * 0.08f;
            Assert.That(Vector3.Distance(expected, lastShotInfo.spawnPosition), Is.LessThan(PositionTolerance));
            Object.Destroy(wall);
        }
    }
}
