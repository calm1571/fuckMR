using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class Test5_MC_CalibrationPhaseSyncTests : Test5_SynchronizationTestFixture
    {
        [Test]
        public void MC_01_ClientConfirm_Advances_To_HostAdjustClient()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetCalibrationPhase(bootstrap, "ClientAdjustHost");

            InvokePrivateMethod(bootstrap, "OnRemoteAlignmentReceived", new RemoteAlignmentPayload
            {
                senderRole = NetworkRole.Client.ToString(),
                stage = "ClientAdjustHost",
                confirmed = true,
                position = Vector3.zero,
                rotation = Quaternion.identity
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_clientAlignmentConfirmed"));
            Assert.AreEqual("HostAdjustClient", GetCalibrationPhase(bootstrap));
        }

        [Test]
        public void MC_02_HostConfirm_Advances_To_SpectatorAdjustClient()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Client);
            SetCalibrationPhase(bootstrap, "HostAdjustClient");

            InvokePrivateMethod(bootstrap, "OnRemoteAlignmentReceived", new RemoteAlignmentPayload
            {
                senderRole = NetworkRole.Host.ToString(),
                stage = "HostAdjustClient",
                confirmed = true,
                position = Vector3.zero,
                rotation = Quaternion.identity
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_hostAlignmentConfirmed"));
            Assert.AreEqual("SpectatorAdjustClient", GetCalibrationPhase(bootstrap));
        }

        [Test]
        public void MC_03_SpectatorClientConfirm_Advances_To_SpectatorAdjustHost()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);
            SetCalibrationPhase(bootstrap, "SpectatorAdjustClient");

            InvokePrivateMethod(bootstrap, "OnRemoteAlignmentReceived", new RemoteAlignmentPayload
            {
                senderRole = NetworkRole.Spectator.ToString(),
                stage = "SpectatorAdjustClient",
                confirmed = true,
                position = Vector3.zero,
                rotation = Quaternion.identity
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_spectatorClientAlignmentConfirmed"));
            Assert.AreEqual("SpectatorAdjustHost", GetCalibrationPhase(bootstrap));
        }

        [Test]
        public void MC_04_SpectatorHostConfirm_Advances_To_HostFinalConfirm()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Client);
            SetCalibrationPhase(bootstrap, "SpectatorAdjustHost");

            InvokePrivateMethod(bootstrap, "OnRemoteAlignmentReceived", new RemoteAlignmentPayload
            {
                senderRole = NetworkRole.Spectator.ToString(),
                stage = "SpectatorAdjustHost",
                confirmed = true,
                position = Vector3.zero,
                rotation = Quaternion.identity
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_spectatorHostAlignmentConfirmed"));
            Assert.AreEqual("HostFinalConfirm", GetCalibrationPhase(bootstrap));
        }
    }
}
