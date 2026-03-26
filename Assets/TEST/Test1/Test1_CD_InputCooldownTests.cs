using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public sealed class M1InputCooldownTests : M1InputTestFixture
    {
        [UnityTest]
        public IEnumerator CD_01_Cast_During_Cooldown_Is_Rejected()
        {
            input.EmitTriggerDown();
            yield return null;

            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(1, shotEventCount);
            Assert.AreEqual(1, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator CD_02_Cast_Just_Before_Cooldown_End_Is_Rejected()
        {
            input.EmitTriggerDown();
            yield return new WaitForSeconds(0.045f);

            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(1, shotEventCount);
        }

        [UnityTest]
        public IEnumerator CD_03_Cast_After_Cooldown_End_Is_Allowed()
        {
            input.EmitTriggerDown();
            yield return new WaitForSeconds(0.06f);

            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(2, shotEventCount);
            Assert.AreEqual(2, CountProjectiles());
        }

        [UnityTest]
        public IEnumerator CD_04_Cooldown_Boundary_Does_Not_Allow_Early_And_Does_Not_Lock_Out()
        {
            input.EmitTriggerDown();
            yield return new WaitForSeconds(0.049f);

            input.EmitTriggerDown();
            yield return null;
            Assert.AreEqual(1, shotEventCount);

            yield return new WaitForSeconds(0.01f);
            input.EmitTriggerDown();
            yield return null;

            Assert.AreEqual(2, shotEventCount);
        }

        [UnityTest]
        public IEnumerator CD_05_Repeated_Cycles_Remain_Stable()
        {
            const int cycles = 20;
            for (var i = 0; i < cycles; i++)
            {
                input.EmitTriggerDown();
                yield return new WaitForSeconds(0.06f);
            }

            Assert.AreEqual(cycles, shotEventCount);
        }
    }
}
