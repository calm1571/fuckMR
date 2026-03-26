using NUnit.Framework;
using Project.Core;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public sealed class Test5_MW_WallSynchronizationTests : Test5_SynchronizationTestFixture
    {
        [Test]
        public void MW_01_Host_In_Playing_Accepts_WallSpawnRequest_And_Creates_Authoritative_State()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Host, AppStateId.Playing);

            InvokePrivateMethod(bootstrap, "OnObstacleSpawnRequestReceived", new ObstacleSpawnRequestPayload
            {
                anchorType = "ArenaCenter",
                localOffset = new Vector3(0.2f, 0f, 0.4f),
                yawOffset = 20f
            });

            var states = GetObstacleStates(bootstrap);
            Assert.AreEqual(1, states.Count);
            Assert.IsTrue(states.ContainsKey(1));
            Assert.IsTrue(states[1].active);
            Assert.AreEqual(spectatorConfig.wallMaxHp, states[1].currentHp, FloatTolerance);
        }

        [Test]
        public void MW_05_NonHost_Or_NonPlaying_Request_Does_Not_Create_Wall_State()
        {
            var bootstrap = CreateBootstrapWithState(NetworkRole.Client, AppStateId.Playing);

            InvokePrivateMethod(bootstrap, "OnObstacleSpawnRequestReceived", new ObstacleSpawnRequestPayload
            {
                anchorType = "ArenaCenter",
                localOffset = Vector3.zero,
                yawOffset = 0f
            });

            Assert.AreEqual(0, GetObstacleStates(bootstrap).Count);

            var hostButNotPlaying = CreateBootstrapWithState(NetworkRole.Host, AppStateId.Result, AppStateId.Playing);
            InvokePrivateMethod(hostButNotPlaying, "OnObstacleSpawnRequestReceived", new ObstacleSpawnRequestPayload
            {
                anchorType = "ArenaCenter",
                localOffset = Vector3.zero,
                yawOffset = 0f
            });

            Assert.AreEqual(0, GetObstacleStates(hostButNotPlaying).Count);
        }
    }
}
