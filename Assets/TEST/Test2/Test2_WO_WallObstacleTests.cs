using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class M0WallObstacleTests : M0CombatTestFixture
    {
        [Test]
        public void WO_01_ApplyObstacleDamage_Reduces_WallHp_By_Configured_Damage()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            var obstacleState = new ObstacleStatePayload
            {
                obstacleId = 1,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                size = spectatorConfig.wallSize,
                currentHp = 50f,
                maxHp = 50f,
                active = true
            };

            GetObstacleStates(bootstrap)[obstacleState.obstacleId] = obstacleState;

            InvokePrivateMethod(bootstrap, "ApplyObstacleDamage", obstacleState.obstacleId, 20f);

            var updated = GetObstacleStates(bootstrap)[obstacleState.obstacleId];
            Assert.AreEqual(30f, updated.currentHp, FloatTolerance);
            Assert.IsTrue(updated.active);
            Assert.AreEqual(1, GetObstacleVisuals(bootstrap).Count);
        }

        [Test]
        public void WO_02_ApplyObstacleDamage_To_Zero_Removes_Obstacle_State_And_Visual()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            var obstacleState = new ObstacleStatePayload
            {
                obstacleId = 2,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                size = spectatorConfig.wallSize,
                currentHp = 10f,
                maxHp = 50f,
                active = true
            };

            GetObstacleStates(bootstrap)[obstacleState.obstacleId] = obstacleState;

            InvokePrivateMethod(bootstrap, "ApplyObstacleDamage", obstacleState.obstacleId, 20f);

            Assert.IsFalse(GetObstacleStates(bootstrap).ContainsKey(obstacleState.obstacleId));
            Assert.IsFalse(GetObstacleVisuals(bootstrap).ContainsKey(obstacleState.obstacleId));
        }
    }
}
