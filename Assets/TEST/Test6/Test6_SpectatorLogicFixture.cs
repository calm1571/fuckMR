using NUnit.Framework;
using Project.Core;
using Project.Networking;
using UnityEngine;

namespace Project.Tests.EditMode
{
    public abstract class Test6_SpectatorLogicFixture : Test5_SynchronizationTestFixture
    {
        [TearDown]
        public void TearDownSpectatorObjects()
        {
            DestroyIfExists("SpectatorBarrageRoot");
            DestroyIfExists("SpectatorBarrageCamera");
        }

        protected SpectatorBarrageView CreateBarrageView()
        {
            var cameraRoot = new GameObject("SpectatorBarrageCamera");
            cameraRoot.transform.position = Vector3.zero;
            cameraRoot.transform.rotation = Quaternion.identity;
            return new SpectatorBarrageView(cameraRoot.transform, 1.2f, Vector3.zero);
        }

        protected static int GetBarrageEntryCount(SpectatorBarrageView view)
        {
            var entries = GetPrivateField<System.Collections.ICollection>(view, "_entries");
            return entries.Count;
        }

        private static void DestroyIfExists(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
