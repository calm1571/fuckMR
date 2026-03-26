using NUnit.Framework;
using Project.Core;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class Test6_SS_SpectatorStateDisplayTests : Test6_SpectatorLogicFixture
    {
        [Test]
        public void SS_01_HpUpdate_Refreshes_Spectator_Local_Host_And_Client_Hp()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            SetPrivateField(bootstrap, "_hostHp", 100);
            SetPrivateField(bootstrap, "_clientHp", 100);

            InvokePrivateMethod(bootstrap, "OnHpUpdateReceived", new HpUpdatePayload
            {
                hostHp = 70,
                clientHp = 55
            });

            Assert.AreEqual(70, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(55, GetPrivateField<int>(bootstrap, "_clientHp"));
        }

        [Test]
        public void SS_04_MatchResult_Formats_Spectator_Result_Text_And_Changes_To_Result_State()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Spectator, AppStateId.Playing, AppStateId.Result);

            InvokePrivateMethod(bootstrap, "OnMatchResultReceived", new MatchResultPayload
            {
                winnerRole = "Host"
            });

            Assert.AreEqual("HOST WIN", GetPrivateField<string>(bootstrap, "_resultText"));
            Assert.AreEqual(AppStateId.Result, GetPrivateField<AppStateMachine>(bootstrap, "_stateMachine").CurrentId);
        }

        [Test]
        public void SS_05_ObstacleState_Is_Applied_On_Spectator_Receiver()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);

            InvokePrivateMethod(bootstrap, "OnObstacleStateReceived", new ObstacleStatePayload
            {
                obstacleId = 9,
                position = UnityEngine.Vector3.zero,
                rotation = UnityEngine.Quaternion.identity,
                size = spectatorConfig.wallSize,
                currentHp = 35f,
                maxHp = 50f,
                active = true
            });

            var states = GetObstacleStates(bootstrap);
            Assert.AreEqual(1, states.Count);
            Assert.IsTrue(states.ContainsKey(9));
            Assert.AreEqual(35f, states[9].currentHp, FloatTolerance);
        }
    }
}
