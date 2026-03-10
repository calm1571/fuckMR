using UnityEngine;

namespace Project.MRWorld
{
    public enum MarkerTrackingSourceMode
    {
        Unknown = 0,
        AutoAprilTag = 1,
        Manual = 2
    }

    public struct MarkerTrackingSample
    {
        public bool hasPose;
        public bool isLocked;
        public float stability01;
        public Pose pose;
        public MarkerTrackingSourceMode sourceMode;
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
