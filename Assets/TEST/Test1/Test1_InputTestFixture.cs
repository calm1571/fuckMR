using System.Collections;
using NUnit.Framework;
using Project.Gameplay.Combat;
using Project.Gameplay.Input;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public abstract class M1InputTestFixture
    {
        protected sealed class FakeInputSource : IPlayerInputSource
        {
            public event System.Action TriggerDown;
            public event System.Action TriggerUp;
            public event System.Action AButtonDown;
            public event System.Action AButtonUp;

            public bool IsDeviceReady => true;

            public void Tick()
            {
            }

            public void EmitTriggerDown() => TriggerDown?.Invoke();
            public void EmitTriggerUp() => TriggerUp?.Invoke();
            public void EmitAButtonDown() => AButtonDown?.Invoke();
            public void EmitAButtonUp() => AButtonUp?.Invoke();
        }

        protected GameObject root;
        protected M1ProjectileShooter shooter;
        protected Transform shootOrigin;
        protected FakeInputSource input;
        protected int shotEventCount;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject($"{GetType().Name}_Root");
            shooter = root.AddComponent<M1ProjectileShooter>();

            var originGo = new GameObject("ShootOrigin");
            originGo.transform.SetParent(root.transform, false);
            originGo.transform.localPosition = Vector3.zero;
            originGo.transform.localRotation = Quaternion.identity;
            shootOrigin = originGo.transform;

            input = new FakeInputSource();
            shooter.SetShootOrigin(shootOrigin);
            shooter.SetCombatTuning(speed: 6f, radius: 0.05f, cooldown: 0.05f);
            shooter.SetShootingEnabled(true);
            shooter.Bind(input);
            shooter.ShotFired += OnShotFired;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (shooter != null)
            {
                shooter.ShotFired -= OnShotFired;
                shooter.Unbind();
            }

            foreach (var projectile in Object.FindObjectsOfType<M1Projectile>())
            {
                if (projectile != null)
                {
                    Object.Destroy(projectile.gameObject);
                }
            }

            if (root != null)
            {
                Object.Destroy(root);
            }

            yield return null;
        }

        private void OnShotFired(M1ProjectileShooter.ShotInfo _)
        {
            shotEventCount++;
        }

        protected static int CountProjectiles()
        {
            return Object.FindObjectsOfType<M1Projectile>().Length;
        }
    }
}
