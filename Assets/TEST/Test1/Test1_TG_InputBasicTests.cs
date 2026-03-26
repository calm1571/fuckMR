using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class M1TriggerInputBasicTests : M1InputTestFixture
    {
        [UnityTest]
        public IEnumerator TG_01_TriggerDown_Generates_Exactly_One_Cast()
        {
            input.EmitTriggerDown();
            input.EmitTriggerUp();
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator TG_02_TriggerRelease_Does_Not_Generate_Extra_Cast()
        {
            input.EmitTriggerDown();
            yield return null;

            var countAfterDown = shotEventCount;

            input.EmitTriggerUp();
            yield return null;

            Assert.AreEqual(countAfterDown, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator TG_03_No_Cast_While_Shooting_Disabled()
        {
            shooter.SetShootingEnabled(false);

            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(0, shotEventCount);
            Assert.AreEqual(0, CountProjectiles());
        }
    }
}
