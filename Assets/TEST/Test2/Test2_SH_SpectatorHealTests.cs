using NUnit.Framework;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class M0SpectatorHealTests : M0CombatTestFixture
    {
        [Test]
        public void SH_01_SpectatorHeal_Host_Increases_HostHp_And_Caps_At_Max()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetPrivateField(bootstrap, "_hostHp", 90);
            SetPrivateField(bootstrap, "_clientHp", 100);

            InvokePrivateMethod(bootstrap, "OnSpectatorVoteReceived", new SpectatorVotePayload
            {
                targetRole = NetworkRole.Host.ToString()
            });

            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_clientHp"));
            Assert.AreEqual(3f, GetPrivateField<float>(bootstrap, "_hostSpectatorVoteCooldownUntil"), FloatTolerance);
        }

        [Test]
        public void SH_02_SpectatorHeal_Cooldown_Blocks_Immediate_Second_Heal()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetPrivateField(bootstrap, "_hostHp", 60);

            InvokePrivateMethod(bootstrap, "OnSpectatorVoteReceived", new SpectatorVotePayload
            {
                targetRole = NetworkRole.Host.ToString()
            });
            InvokePrivateMethod(bootstrap, "OnSpectatorVoteReceived", new SpectatorVotePayload
            {
                targetRole = NetworkRole.Host.ToString()
            });

            Assert.AreEqual(75, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(3f, GetPrivateField<float>(bootstrap, "_hostSpectatorVoteCooldownUntil"), FloatTolerance);
        }

        [Test]
        public void SH_03_SpectatorHeal_Invalid_TargetRole_Is_Ignored()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetPrivateField(bootstrap, "_hostHp", 70);
            SetPrivateField(bootstrap, "_clientHp", 80);

            InvokePrivateMethod(bootstrap, "OnSpectatorVoteReceived", new SpectatorVotePayload
            {
                targetRole = "InvalidRole"
            });

            Assert.AreEqual(70, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(80, GetPrivateField<int>(bootstrap, "_clientHp"));
            Assert.AreEqual(0f, GetPrivateField<float>(bootstrap, "_hostSpectatorVoteCooldownUntil"), FloatTolerance);
        }
    }
}
