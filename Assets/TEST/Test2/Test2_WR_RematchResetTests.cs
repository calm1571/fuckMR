using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class M0RematchResetTests : M0CombatTestFixture
    {
        [Test]
        public void WR_01_ResetCombatForNewMatch_Restores_Hp_Cooldowns_And_Clears_Walls()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetPrivateField(bootstrap, "_hostHp", 35);
            SetPrivateField(bootstrap, "_clientHp", 45);
            SetPrivateField(bootstrap, "_hostShieldEndTime", 2f);
            SetPrivateField(bootstrap, "_clientShieldEndTime", 3f);
            SetPrivateField(bootstrap, "_hostShieldCooldownUntil", 4f);
            SetPrivateField(bootstrap, "_clientShieldCooldownUntil", 5f);
            SetPrivateField(bootstrap, "_hostNextShootAllowedTime", 6f);
            SetPrivateField(bootstrap, "_clientNextShootAllowedTime", 7f);
            SetPrivateField(bootstrap, "_localShootCooldownUntil", 8f);
            SetPrivateField(bootstrap, "_localSpectatorVoteCooldownUntil", 9f);
            SetPrivateField(bootstrap, "_hostSpectatorVoteCooldownUntil", 10f);
            SetPrivateField(bootstrap, "_hostWallSpawnCooldownUntil", 11f);
            SetPrivateField(bootstrap, "_hostObstacleStateBroadcastCooldownUntil", 12f);
            SetPrivateField(bootstrap, "_resultText", "Client Wins");

            var obstacleState = new ObstacleStatePayload
            {
                obstacleId = 3,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                size = spectatorConfig.wallSize,
                currentHp = 40f,
                maxHp = 50f,
                active = true
            };

            InvokePrivateMethod(bootstrap, "ApplyObstacleState", obstacleState);
            InvokePrivateMethod(bootstrap, "ResetCombatForNewMatch");

            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_clientHp"));
            Assert.AreEqual(-1f, GetPrivateField<float>(bootstrap, "_hostShieldEndTime"), FloatTolerance);
            Assert.AreEqual(-1f, GetPrivateField<float>(bootstrap, "_clientShieldEndTime"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostShieldCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_clientShieldCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostNextShootAllowedTime"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_clientNextShootAllowedTime"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_localShootCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_localSpectatorVoteCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostSpectatorVoteCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostWallSpawnCooldownUntil"), FloatTolerance);
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostObstacleStateBroadcastCooldownUntil"), FloatTolerance);
            Assert.AreEqual("Result", GetPrivateField<string>(bootstrap, "_resultText"));
            Assert.AreEqual(0, GetObstacleStates(bootstrap).Count);
            Assert.AreEqual(0, GetObstacleVisuals(bootstrap).Count);
        }
    }
}
