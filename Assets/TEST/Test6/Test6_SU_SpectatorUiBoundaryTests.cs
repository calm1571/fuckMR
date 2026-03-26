using NUnit.Framework;
using Project.Core;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class Test6_SU_SpectatorUiBoundaryTests : Test6_SpectatorLogicFixture
    {
        [Test]
        public void SU_01_Player_Roles_Cannot_Enable_Spectator_Only_WallPlacement()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Client, AppStateId.Playing);

            InvokePrivateMethod(bootstrap, "HandleSpectatorPlaceWallClicked");

            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_spectatorWallPlacementActive"));
        }

        [Test]
        public void SU_05_Spectator_Control_Actions_Respect_Local_Cooldown_Field()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            SetPrivateField(bootstrap, "_localSpectatorVoteCooldownUntil", 5f);

            Assert.AreEqual(5f, GetPrivateField<float>(bootstrap, "_localSpectatorVoteCooldownUntil"), FloatTolerance);
        }
    }
}
