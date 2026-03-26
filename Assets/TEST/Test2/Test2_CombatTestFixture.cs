using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project.Core;
using Project.Gameplay.Combat;
using Project.Networking;
using Project.ScriptableObjects;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public abstract class M0CombatTestFixture
    {
        protected const float FloatTolerance = 0.001f;

        protected GameObject bootstrapObject;
        protected GameObject obstacleRootObject;
        protected CombatBalanceConfig combatConfig;
        protected SpectatorSupportConfig spectatorConfig;

        [SetUp]
        public void SetUp()
        {
            SetStaticField(typeof(M0RuntimeBootstrap), "_instance", null);

            combatConfig = ScriptableObject.CreateInstance<CombatBalanceConfig>();
            combatConfig.hp = 100;
            combatConfig.damage = 20;

            spectatorConfig = ScriptableObject.CreateInstance<SpectatorSupportConfig>();
            spectatorConfig.healAmount = 15;
            spectatorConfig.voteCooldown = 3f;
            spectatorConfig.wallMaxHp = 50;
            spectatorConfig.wallShotDamage = 20;
            spectatorConfig.wallSize = new Vector3(1.6f, 1.35f, 0.12f);

            obstacleRootObject = new GameObject("TestObstacleRoot");
            bootstrapObject = new GameObject("TestBootstrap");
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticField(typeof(M0RuntimeBootstrap), "_instance", null);

            if (bootstrapObject != null)
            {
                Object.DestroyImmediate(bootstrapObject);
            }

            if (obstacleRootObject != null)
            {
                Object.DestroyImmediate(obstacleRootObject);
            }

            if (combatConfig != null)
            {
                Object.DestroyImmediate(combatConfig);
            }

            if (spectatorConfig != null)
            {
                Object.DestroyImmediate(spectatorConfig);
            }
        }

        protected M0RuntimeBootstrap CreateBootstrap(NetworkRole role)
        {
            var bootstrap = bootstrapObject.AddComponent<M0RuntimeBootstrap>();
            SetPrivateField(bootstrap, "combatBalanceConfig", combatConfig);
            SetPrivateField(bootstrap, "spectatorSupportConfig", spectatorConfig);
            SetPrivateField(bootstrap, "_selectedRole", role);
            SetPrivateField(bootstrap, "_obstacleVisualRoot", obstacleRootObject.transform);
            return bootstrap;
        }

        protected static Dictionary<int, ObstacleStatePayload> GetObstacleStates(M0RuntimeBootstrap bootstrap)
        {
            return GetPrivateField<Dictionary<int, ObstacleStatePayload>>(bootstrap, "_obstacleStates");
        }

        protected static Dictionary<int, WallObstacleRuntime> GetObstacleVisuals(M0RuntimeBootstrap bootstrap)
        {
            return GetPrivateField<Dictionary<int, WallObstacleRuntime>>(bootstrap, "_obstacleVisuals");
        }

        protected static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Failed to find private field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        protected static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Failed to find private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        protected static void SetStaticField(System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Failed to find static field '{fieldName}'.");
            field.SetValue(null, value);
        }

        protected static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Failed to find private method '{methodName}'.");
            return method.Invoke(target, args);
        }
    }
}
