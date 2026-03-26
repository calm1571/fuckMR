using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class M3MessageRoutingTests : M3NetworkCoordinatorTestFixture
    {
        [Test]
        public void RT_05_RematchReady_Routes_To_Pending_Rematch()
        {
            var coordinator = CreateCoordinator(NetworkRole.Client);
            var payload = new RematchReadyPayload { ready = true };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.RematchReady, payload, "remote-client", NetworkRole.Host));

            Assert.IsTrue(GetPrivateField<bool>(coordinator, "_remoteRematchReadyRequested"));
            Assert.NotNull(GetPrivateField<RematchReadyPayload>(coordinator, "_pendingRematchReady"));
            Assert.IsTrue(GetPrivateField<RematchReadyPayload>(coordinator, "_pendingRematchReady").ready);
        }

        [Test]
        public void RT_06_SpectatorVote_Routes_Only_For_Host_From_Spectator()
        {
            var coordinator = CreateCoordinator(NetworkRole.Host);
            var payload = new SpectatorVotePayload { targetRole = NetworkRole.Client.ToString() };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.SpectatorVote, payload, "remote-spectator", NetworkRole.Spectator));

            Assert.IsTrue(GetPrivateField<bool>(coordinator, "_remoteSpectatorVoteRequested"));
            Assert.NotNull(GetPrivateField<SpectatorVotePayload>(coordinator, "_pendingSpectatorVote"));
            Assert.AreEqual(NetworkRole.Client.ToString(), GetPrivateField<SpectatorVotePayload>(coordinator, "_pendingSpectatorVote").targetRole);
        }

        [Test]
        public void RT_07_ObstacleSpawnRequest_Routes_Only_For_Host_From_Spectator()
        {
            var coordinator = CreateCoordinator(NetworkRole.Host);
            var payload = new ObstacleSpawnRequestPayload
            {
                anchorType = "ArenaCenter",
                localOffset = new Vector3(0.1f, 0f, 0.2f),
                yawOffset = 20f
            };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.ObstacleSpawnRequest, payload, "remote-spectator", NetworkRole.Spectator));

            Assert.IsTrue(GetPrivateField<bool>(coordinator, "_remoteObstacleSpawnRequestRequested"));
            var pending = GetPrivateField<ObstacleSpawnRequestPayload>(coordinator, "_pendingObstacleSpawnRequest");
            Assert.NotNull(pending);
            Assert.AreEqual(payload.anchorType, pending.anchorType);
            AssertVector3Equal(payload.localOffset, pending.localOffset);
            Assert.AreEqual(payload.yawOffset, pending.yawOffset, FloatTolerance);
        }

        [Test]
        public void RT_08_ObstacleState_Routes_For_NonHost_Receiver()
        {
            var coordinator = CreateCoordinator(NetworkRole.Client);
            var payload = new ObstacleStatePayload
            {
                obstacleId = 11,
                position = new Vector3(2f, 0.7f, 1f),
                rotation = Quaternion.Euler(0f, 15f, 0f),
                size = new Vector3(1.6f, 1.35f, 0.12f),
                currentHp = 60f,
                maxHp = 100f,
                active = true
            };

            InvokeOnMessageReceived(coordinator, BuildMessage(LanMessageTypes.ObstacleState, payload, "remote-host", NetworkRole.Host));

            Assert.IsTrue(GetPrivateField<bool>(coordinator, "_remoteObstacleStateRequested"));
            var pending = GetPrivateField<ObstacleStatePayload>(coordinator, "_pendingObstacleState");
            Assert.NotNull(pending);
            Assert.AreEqual(payload.obstacleId, pending.obstacleId);
            Assert.AreEqual(payload.active, pending.active);
        }
    }
}
