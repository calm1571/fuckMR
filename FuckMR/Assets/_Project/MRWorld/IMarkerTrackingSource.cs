using UnityEngine;

namespace Project.MRWorld
{
    public struct MarkerTrackingSample
    {
        public bool hasPose;
        public bool isLocked;
        public float stability01;
        public Pose pose;
    }

    public interface IMarkerTrackingSource
    {
        void Begin();
        void End();
        void Tick(float deltaTime);
        bool TryGetSample(out MarkerTrackingSample sample);
        string BuildDebugText();
    }
}
