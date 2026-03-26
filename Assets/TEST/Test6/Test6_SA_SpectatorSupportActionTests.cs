using NUnit.Framework;
using Project.Core;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class Test6_SA_SpectatorSupportActionTests : Test6_SpectatorLogicFixture
    {
        [Test]
        public void SA_04_PlaceWall_Click_Outside_Playing_Does_Not_Enable_Placement()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Spectator, AppStateId.Result, AppStateId.Playing);

            InvokePrivateMethod(bootstrap, "HandleSpectatorPlaceWallClicked");

            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_spectatorWallPlacementActive"));
        }

        [Test]
        public void SA_04_PlaceWall_Click_For_NonSpectator_Does_Not_Enable_Placement()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Host, AppStateId.Playing);

            InvokePrivateMethod(bootstrap, "HandleSpectatorPlaceWallClicked");

            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_spectatorWallPlacementActive"));
        }
    }
}
