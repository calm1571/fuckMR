// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines configurable AprilTag tracking parameters.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.MRWorld
{
    public readonly struct AprilTagTrackingConfig
    {
        public readonly string family;
        public readonly int id;
        public readonly float tagSizeMeters;
        public readonly int frameWidth;
        public readonly int frameHeight;

        public AprilTagTrackingConfig(string family, int id, float tagSizeMeters, int frameWidth, int frameHeight)
        {
            this.family = string.IsNullOrWhiteSpace(family) ? "36h11" : family.Trim();
            this.id = Mathf.Max(0, id);
            this.tagSizeMeters = Mathf.Max(0.01f, tagSizeMeters);
            this.frameWidth = Mathf.Clamp(frameWidth, 320, 1920);
            this.frameHeight = Mathf.Clamp(frameHeight, 240, 1080);
        }
    }
}


