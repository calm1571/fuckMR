using NUnit.Framework;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class Test7_RL_ResyncGateTests : Test7_AlignmentFixture
    {
        [Test]
        public void RL_01_Client_Can_Adjust_Only_During_ClientAdjustHost_Phase()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Client);
            SetCalibrationPhase(bootstrap, "ClientAdjustHost");

            Assert.IsTrue(InvokeCanAdjustLiveRemoteAlignment(bootstrap));

            SetCalibrationPhase(bootstrap, "HostAdjustClient");
            Assert.IsFalse(InvokeCanAdjustLiveRemoteAlignment(bootstrap));
        }

        [Test]
        public void RL_03_Spectator_Can_Adjust_Only_During_Step3_And_Step4()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);

            SetCalibrationPhase(bootstrap, "SpectatorAdjustClient");
            Assert.IsTrue(InvokeCanAdjustLiveRemoteAlignment(bootstrap));

            SetCalibrationPhase(bootstrap, "SpectatorAdjustHost");
            Assert.IsTrue(InvokeCanAdjustLiveRemoteAlignment(bootstrap));

            SetCalibrationPhase(bootstrap, "HostFinalConfirm");
            Assert.IsFalse(InvokeCanAdjustLiveRemoteAlignment(bootstrap));
        }

        [Test]
        public void RL_04_RemoteAlignment_Ignore_Unconfirmed_Message()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetCalibrationPhase(bootstrap, "ClientAdjustHost");

            InvokePrivateMethod(bootstrap, "OnRemoteAlignmentReceived", new RemoteAlignmentPayload
            {
                senderRole = NetworkRole.Client.ToString(),
                stage = "ClientAdjustHost",
                confirmed = false
            });

            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_clientAlignmentConfirmed"));
            Assert.AreEqual("ClientAdjustHost", GetCalibrationPhase(bootstrap));
        }
    }
}
