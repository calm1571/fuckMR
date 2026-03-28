// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines the interface for marker tracking sources.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.MRWorld
{
        /// <summary>
    /// Marker tracking source mode enumeration.
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
    /// Marker tracking source abstraction interface.
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




