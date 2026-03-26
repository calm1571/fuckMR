using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class Test7_SPC_DisplayBasisTests : Test7_AlignmentFixture
    {
        [Test]
        public void SPC_03_Spectator_DisplayBasis_Uses_Host_And_Client_Proxy_Heads()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Spectator);
            var hostProxy = CreateProxy("HostProxy", new Vector3(-1f, 1.5f, 0f));
            var clientProxy = CreateProxy("ClientProxy", new Vector3(1f, 1.5f, 0f));
            SetPrivateField(bootstrap, "_spectatorHostProxy", hostProxy);
            SetPrivateField(bootstrap, "_spectatorClientProxy", clientProxy);

            var result = InvokeTryGetArenaDisplayBasis(bootstrap, out var center, out var forward, out var right, out var baseYaw);

            Assert.IsTrue(result);
            Assert.That(Vector3.Distance(center, new Vector3(0f, 1.5f, 0f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(forward, Vector3.right), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(right, Vector3.back), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(baseYaw - 90f), Is.LessThan(0.001f));

            DestroyProxyIfAny(hostProxy);
            DestroyProxyIfAny(clientProxy);
        }

        [Test]
        public void SPC_04_NonSpectator_Cannot_Build_Spectator_DisplayBasis()
        {
            var bootstrap = CreateBootstrap(NetworkRole.Host);

            var result = InvokeTryGetArenaDisplayBasis(bootstrap, out _, out _, out _, out _);

            Assert.IsFalse(result);
        }
    }
}
