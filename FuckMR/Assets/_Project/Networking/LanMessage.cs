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
    public sealed class WorldRootSyncPayload
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public sealed class ShootPayload
    {
        public Vector3 spawnPosition;
        public Vector3 direction;
        public float speed;
        public float maxDistance;
        public float lifetime;
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
        public const string WorldRootSync = "WORLD_ROOT_SYNC";
        public const string Shoot = "SHOOT";
    }
}
