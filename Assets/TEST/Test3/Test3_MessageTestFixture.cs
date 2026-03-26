using System.Reflection;
using NUnit.Framework;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public abstract class M3NetworkCoordinatorTestFixture
    {
        protected const float FloatTolerance = 0.001f;

        protected static M3NetworkCoordinator CreateCoordinator(NetworkRole role)
        {
            var coordinator = new M3NetworkCoordinator(7777, "127.0.0.1", 30f);
            SetAutoPropertyBackingField(coordinator, "<Role>k__BackingField", role);
            return coordinator;
        }

        protected static LanMessage BuildMessage(string type, object payload, string playerId, NetworkRole senderRole)
        {
            return new LanMessage
            {
                type = type,
                playerId = playerId,
                senderRole = senderRole.ToString(),
                payload = payload == null ? null : JsonUtility.ToJson(payload)
            };
        }

        protected static void InvokeOnMessageReceived(M3NetworkCoordinator coordinator, LanMessage message)
        {
            var method = typeof(M3NetworkCoordinator).GetMethod("OnMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Failed to find M3NetworkCoordinator.OnMessageReceived via reflection.");
            method.Invoke(coordinator, new object[] { message });
        }

        protected static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Failed to find private field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        protected static void SetAutoPropertyBackingField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Failed to find backing field '{fieldName}'.");
            field.SetValue(target, value);
        }

        protected static void AssertVector3Equal(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, FloatTolerance);
            Assert.AreEqual(expected.y, actual.y, FloatTolerance);
            Assert.AreEqual(expected.z, actual.z, FloatTolerance);
        }

        protected static void AssertQuaternionEqual(Quaternion expected, Quaternion actual)
        {
            Assert.AreEqual(expected.x, actual.x, FloatTolerance);
            Assert.AreEqual(expected.y, actual.y, FloatTolerance);
            Assert.AreEqual(expected.z, actual.z, FloatTolerance);
            Assert.AreEqual(expected.w, actual.w, FloatTolerance);
        }
    }
}
