using NUnit.Framework;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public sealed class M3MessageFaultToleranceTests : M3NetworkCoordinatorTestFixture
    {
        [Test]
        public void EX_03_SelfSent_Message_Is_Filtered()
        {
            var coordinator = CreateCoordinator(NetworkRole.Client);
            var localPlayerId = GetPrivateField<string>(coordinator, "_localPlayerId");
            var payload = new RematchReadyPayload { ready = true };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.RematchReady, payload, localPlayerId, NetworkRole.Client));

            Assert.IsFalse(GetPrivateField<bool>(coordinator, "_remoteRematchReadyRequested"));
            Assert.IsNull(GetPrivateField<RematchReadyPayload>(coordinator, "_pendingRematchReady"));
        }

        [Test]
        public void EX_06_Invalid_SenderRole_For_SpectatorVote_Is_Ignored()
        {
            var coordinator = CreateCoordinator(NetworkRole.Host);
            var payload = new SpectatorVotePayload { targetRole = NetworkRole.Host.ToString() };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.SpectatorVote, payload, "remote-client", NetworkRole.Client));

            Assert.IsFalse(GetPrivateField<bool>(coordinator, "_remoteSpectatorVoteRequested"));
            Assert.IsNull(GetPrivateField<SpectatorVotePayload>(coordinator, "_pendingSpectatorVote"));
        }

        [Test]
        public void EX_08_Incomplete_ObstacleStatePayload_Does_Not_Crash_And_Uses_Defaults()
        {
            var coordinator = CreateCoordinator(NetworkRole.Client);
            var message = new LanMessage
            {
                type = LanMessageTypes.ObstacleState,
                playerId = "remote-host",
                senderRole = NetworkRole.Host.ToString(),
                payload = "{\"obstacleId\":5,\"active\":true}"
            };

            Assert.DoesNotThrow(() => InvokeOnMessageReceived(coordinator, message));
            Assert.IsTrue(GetPrivateField<bool>(coordinator, "_remoteObstacleStateRequested"));
            var pending = GetPrivateField<ObstacleStatePayload>(coordinator, "_pendingObstacleState");
            Assert.NotNull(pending);
            Assert.AreEqual(5, pending.obstacleId);
            Assert.IsTrue(pending.active);
        }
    }
}
