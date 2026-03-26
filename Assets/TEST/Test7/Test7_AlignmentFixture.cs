using NUnit.Framework;
using Project.Core;
using Project.Gameplay.Combat;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public abstract class Test7_AlignmentFixture : Test5_SynchronizationTestFixture
    {
        protected static bool InvokeTryGetArenaDisplayBasis(
            M0RuntimeBootstrap bootstrap,
            out Vector3 center,
            out Vector3 forward,
            out Vector3 right,
            out float baseYaw)
        {
            var args = new object[] { Vector3.zero, Vector3.zero, Vector3.zero, 0f };
            var result = (bool)InvokePrivateMethod(bootstrap, "TryGetArenaDisplayBasis", args);
            center = (Vector3)args[0];
            forward = (Vector3)args[1];
            right = (Vector3)args[2];
            baseYaw = (float)args[3];
            return result;
        }

        protected static bool InvokeCanAdjustLiveRemoteAlignment(M0RuntimeBootstrap bootstrap)
        {
            return (bool)InvokePrivateMethod(bootstrap, "CanAdjustLiveRemoteAlignment");
        }

        protected static void InvokeUpdateLocalCalibrationReady(M0RuntimeBootstrap bootstrap, float now)
        {
            InvokePrivateMethod(bootstrap, "UpdateLocalCalibrationReady", now);
        }

        protected static M3RemotePlayerProxy CreateProxy(string name, Vector3 headPosition)
        {
            var go = new GameObject(name);
            var proxy = go.AddComponent<M3RemotePlayerProxy>();
            proxy.ApplyPose(new PosePayload
            {
                head = new PoseData { position = headPosition, rotation = Quaternion.identity },
                leftHand = new PoseData { position = headPosition + Vector3.left * 0.2f, rotation = Quaternion.identity },
                rightHand = new PoseData { position = headPosition + Vector3.right * 0.2f, rotation = Quaternion.identity }
            });
            return proxy;
        }

        protected static void DestroyProxyIfAny(M3RemotePlayerProxy proxy)
        {
            if (proxy != null)
            {
                Object.DestroyImmediate(proxy.gameObject);
            }
        }
    }
}
