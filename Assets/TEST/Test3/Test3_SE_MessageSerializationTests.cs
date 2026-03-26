using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class M3MessageSerializationTests : M3NetworkCoordinatorTestFixture
    {
        [Test]
        public void SE_06_RematchReadyPayload_RoundTrip_Preserves_Ready()
        {
            var payload = new RematchReadyPayload { ready = true };
            var roundTrip = JsonUtility.FromJson<RematchReadyPayload>(JsonUtility.ToJson(payload));

            Assert.NotNull(roundTrip);
            Assert.IsTrue(roundTrip.ready);
        }

        [Test]
        public void SE_07_SpectatorVotePayload_RoundTrip_Preserves_TargetRole()
        {
            var payload = new SpectatorVotePayload { targetRole = NetworkRole.Host.ToString() };
            var roundTrip = JsonUtility.FromJson<SpectatorVotePayload>(JsonUtility.ToJson(payload));

            Assert.NotNull(roundTrip);
            Assert.AreEqual(NetworkRole.Host.ToString(), roundTrip.targetRole);
        }

        [Test]
        public void SE_08_ObstacleSpawnRequestPayload_RoundTrip_Preserves_Fields()
        {
            var payload = new ObstacleSpawnRequestPayload
            {
                anchorType = "ArenaCenter",
                localOffset = new Vector3(1.2f, 0.5f, -0.8f),
                yawOffset = 35f
            };

            var roundTrip = JsonUtility.FromJson<ObstacleSpawnRequestPayload>(JsonUtility.ToJson(payload));

            Assert.NotNull(roundTrip);
            Assert.AreEqual(payload.anchorType, roundTrip.anchorType);
            AssertVector3Equal(payload.localOffset, roundTrip.localOffset);
            Assert.AreEqual(payload.yawOffset, roundTrip.yawOffset, FloatTolerance);
        }

        [Test]
        public void SE_09_ObstacleStatePayload_RoundTrip_Preserves_Fields()
        {
            var payload = new ObstacleStatePayload
            {
                obstacleId = 7,
                position = new Vector3(1f, 2f, 3f),
                rotation = Quaternion.Euler(0f, 35f, 0f),
                size = new Vector3(1.6f, 1.35f, 0.12f),
                currentHp = 80f,
                maxHp = 100f,
                active = true
            };

            var roundTrip = JsonUtility.FromJson<ObstacleStatePayload>(JsonUtility.ToJson(payload));

            Assert.NotNull(roundTrip);
            Assert.AreEqual(payload.obstacleId, roundTrip.obstacleId);
            AssertVector3Equal(payload.position, roundTrip.position);
            AssertQuaternionEqual(payload.rotation, roundTrip.rotation);
            AssertVector3Equal(payload.size, roundTrip.size);
            Assert.AreEqual(payload.currentHp, roundTrip.currentHp, FloatTolerance);
            Assert.AreEqual(payload.maxHp, roundTrip.maxHp, FloatTolerance);
            Assert.AreEqual(payload.active, roundTrip.active);
        }
    }
}
