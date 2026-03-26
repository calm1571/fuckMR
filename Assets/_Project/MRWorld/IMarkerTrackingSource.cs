using UnityEngine;

namespace Project.MRWorld
{
        /// <summary>
    /// Marker 跟踪源模式枚举。
    /// </summary>
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

        /// <summary>
    /// Marker 跟踪源抽象接口。
    /// </summary>
    public interface IMarkerTrackingSource
    {
        void Begin();
        void End();
        void Tick(float deltaTime);
        bool TryGetSample(out MarkerTrackingSample sample);
        string BuildDebugText();
    }
}

