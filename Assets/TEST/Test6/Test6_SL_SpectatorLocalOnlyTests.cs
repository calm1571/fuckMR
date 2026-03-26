using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class Test6_SL_SpectatorLocalOnlyTests : Test6_SpectatorLogicFixture
    {
        [Test]
        public void SL_01_LocalBarrage_Is_Ignored_For_NonSpectator()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            var barrageView = CreateBarrageView();
            SetPrivateField(bootstrap, "_spectatorBarrageView", barrageView);

            InvokePrivateMethod(bootstrap, "ShowLocalSpectatorBarrage", "COOL");

            Assert.AreEqual(0, GetBarrageEntryCount(barrageView));
        }

        [Test]
        public void SL_01_LocalBarrage_Creates_Local_Entry_For_Spectator()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            var barrageView = CreateBarrageView();
            SetPrivateField(bootstrap, "_spectatorBarrageView", barrageView);

            InvokePrivateMethod(bootstrap, "ShowLocalSpectatorBarrage", "COOL");

            Assert.AreEqual(1, GetBarrageEntryCount(barrageView));
        }

        [Test]
        public void SL_03_LocalBarrage_Does_Not_Alter_Hp_Or_Wall_State()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            var barrageView = CreateBarrageView();
            SetPrivateField(bootstrap, "_spectatorBarrageView", barrageView);
            SetPrivateField(bootstrap, "_hostHp", 88);
            SetPrivateField(bootstrap, "_clientHp", 66);

            InvokePrivateMethod(bootstrap, "ShowLocalSpectatorBarrage", "NICE SHOT");

            Assert.AreEqual(88, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(66, GetPrivateField<int>(bootstrap, "_clientHp"));
            Assert.AreEqual(0, GetObstacleStates(bootstrap).Count);
            Assert.AreEqual(1, GetBarrageEntryCount(barrageView));
        }
    }
}
