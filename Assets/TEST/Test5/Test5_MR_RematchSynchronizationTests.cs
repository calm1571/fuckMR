using NUnit.Framework;
using Project.Core;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class Test5_MR_RematchSynchronizationTests : Test5_SynchronizationTestFixture
    {
        [Test]
        public void MR_04_Host_Rematch_Handshake_Transitions_Result_To_Playing()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Host, AppStateId.Result, AppStateId.Playing);
            SetPrivateField(bootstrap, "_localRematchReady", true);
            SetPrivateField(bootstrap, "_hostHp", 20);
            SetPrivateField(bootstrap, "_clientHp", 30);

            InvokePrivateMethod(bootstrap, "OnRemoteRematchReadyReceived", new RematchReadyPayload
            {
                ready = true
            });

            Assert.IsTrue(GetPrivateField<bool>(bootstrap, "_remoteRematchReady"));
            Assert.AreEqual(AppStateId.Playing, GetPrivateField<AppStateMachine>(bootstrap, "_stateMachine").CurrentId);
            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(100, GetPrivateField<int>(bootstrap, "_clientHp"));
        }

        [Test]
        public void MR_05_Rematch_Ready_False_Does_Not_Start_New_Round()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Host, AppStateId.Result, AppStateId.Playing);
            SetPrivateField(bootstrap, "_localRematchReady", true);
            SetPrivateField(bootstrap, "_hostHp", 20);
            SetPrivateField(bootstrap, "_clientHp", 30);

            InvokePrivateMethod(bootstrap, "OnRemoteRematchReadyReceived", new RematchReadyPayload
            {
                ready = false
            });

            Assert.IsFalse(GetPrivateField<bool>(bootstrap, "_remoteRematchReady"));
            Assert.AreEqual(AppStateId.Result, GetPrivateField<AppStateMachine>(bootstrap, "_stateMachine").CurrentId);
            Assert.AreEqual(20, GetPrivateField<int>(bootstrap, "_hostHp"));
            Assert.AreEqual(30, GetPrivateField<int>(bootstrap, "_clientHp"));
        }
    }
}
