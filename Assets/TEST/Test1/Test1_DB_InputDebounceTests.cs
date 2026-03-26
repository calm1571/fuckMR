using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class M1InputDebounceTests : M1InputTestFixture
    {
        [UnityTest]
        public IEnumerator DB_01_Rapid_Jitter_Inside_Cooldown_Does_Not_Create_Extra_Cast()
        {
            input.EmitTriggerDown();
            yield return new WaitForSeconds(0.005f);

            input.EmitTriggerUp();
            input.EmitTriggerDown();
            input.EmitTriggerUp();
            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator DB_02_Hold_To_Cast_Triggers_Only_Once_Per_Press()
        {
            input.EmitTriggerDown();
            yield return null;

            for (var i = 0; i < 10; i++)
            {
                yield return null;
            }

            input.EmitTriggerUp();
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }
    }
}
