// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines the interface for AprilTag detector implementations.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using UnityEngine;

namespace Project.MRWorld
{
    public readonly struct AprilTagCameraIntrinsics
    {
        public readonly float fx;
        public readonly float fy;
        public readonly float cx;
        public readonly float cy;

        public AprilTagCameraIntrinsics(float fx, float fy, float cx, float cy)
        {
            this.fx = fx;
            this.fy = fy;
            this.cx = cx;
            this.cy = cy;
        }
    }

    public readonly struct AprilTagDetectionResult
    {
        public readonly bool hasPose;
        public readonly int id;
        public readonly Pose tagPoseInCamera;
        public readonly float confidence;

        public AprilTagDetectionResult(bool hasPose, int id, Pose tagPoseInCamera, float confidence)
        {
            this.hasPose = hasPose;
            this.id = id;
            this.tagPoseInCamera = tagPoseInCamera;
            this.confidence = Mathf.Clamp01(confidence);
        }
    }

        /// <summary>
    /// AprilTag 检测器抽象接口。
    /// </summary>
    public interface IAprilTagDetector
    {
        bool IsAvailable { get; }
        string DebugName { get; }
        bool TryDetect(
            IntPtr rgbaBuffer,
            int width,
            int height,
            int strideBytes,
            in AprilTagCameraIntrinsics intrinsics,
            in AprilTagTrackingConfig config,
            out AprilTagDetectionResult result);
    }
}



