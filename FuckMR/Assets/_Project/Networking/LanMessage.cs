using System;
using UnityEngine;

namespace Project.Networking
{
    [Serializable]
    public sealed class LanMessage
    {
        public string type;
        public string playerId;
        public string payload;
    }

    [Serializable]
    public sealed class PosePayload
    {
        public PoseData head;
        public PoseData leftHand;
        public PoseData rightHand;
    }

    [Serializable]
    public struct PoseData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public static class LanMessageTypes
    {
        public const string Hello = "HELLO";
        public const string HelloAck = "HELLO_ACK";
        public const string Pose = "POSE";
        public const string StartCalibration = "START_CALIBRATION";
    }
}
