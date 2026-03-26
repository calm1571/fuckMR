using NUnit.Framework;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class Test7_AR_AlignmentReadyTests : Test7_AlignmentFixture
    {
        [Test]
        public void AR_01_RemoteCalibrationReady_Payload_Updates_RemoteReady_Flag()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);

            InvokePrivateMethod(bootstrap, "OnRemoteCalibrationReadyReceived", new CalibrationReadyPayload
            {
                ready = true
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_remoteCalibrationReady"));
        }

        [Test]
        public void AR_03_Spectator_LocalReady_Requires_Both_Serial_Alignment_Confirms()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            SetPrivateField(bootstrap, "_spectatorClientAlignmentConfirmed", true);
            SetPrivateField(bootstrap, "_spectatorHostAlignmentConfirmed", false);

            InvokeUpdateLocalCalibrationReady(bootstrap, 1f);
            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_localCalibrationReady"));

            SetPrivateField(bootstrap, "_spectatorHostAlignmentConfirmed", true);
            InvokeUpdateLocalCalibrationReady(bootstrap, 2f);

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_localCalibrationReady"));
            Assert.AreEqual(2f, GetPrivateField<float>(bootstrap, "_localCalibrationReadySince"), FloatTolerance);
        }

        [Test]
        public void AR_05_FinalPhase_Ready_Requires_All_Four_Confirm_Flags()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetCalibrationPhase(bootstrap, "HostFinalConfirm");
            SetPrivateField(bootstrap, "_clientAlignmentConfirmed", true);
            SetPrivateField(bootstrap, "_hostAlignmentConfirmed", true);
            SetPrivateField(bootstrap, "_spectatorClientAlignmentConfirmed", true);
            SetPrivateField(bootstrap, "_spectatorHostAlignmentConfirmed", false);

            InvokeUpdateLocalCalibrationReady(bootstrap, 3f);
            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_localCalibrationReady"));

            SetPrivateField(bootstrap, "_spectatorHostAlignmentConfirmed", true);
            InvokeUpdateLocalCalibrationReady(bootstrap, 4f);

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_localCalibrationReady"));
        }
    }
}
