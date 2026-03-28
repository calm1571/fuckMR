// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines LAN message payloads and serializable network data structures.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using UnityEngine;

namespace Project.Networking
{
    [Serializable]
        /// <summary>
    /// Generic network message envelope.
    /// </summary>
    public sealed class LanMessage
    {
        public string type;
        public string playerId;
        public string senderRole;
        public string payload;
    }

    [Serializable]
        /// <summary>
    /// Head and hand pose synchronization payload.
    /// </summary>
    public sealed class PosePayload
    {
        public PoseData head;
        public PoseData leftHand;
        public PoseData rightHand;
    }

    [Serializable]
        /// <summary>
    /// WorldRoot synchronization payload.
    /// </summary>
    public sealed class WorldRootSyncPayload
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
        /// <summary>
    /// Shoot message payload.
    /// </summary>
    public sealed class ShootPayload
    {
        public Vector3 spawnPosition;
        public Vector3 direction;
        public float speed;
        public float maxDistance;
        public float lifetime;
    }

    [Serializable]
        /// <summary>
    /// Shield activation payload.
    /// </summary>
    public sealed class ShieldPayload
    {
        public bool active;
        public float duration;
    }

    [Serializable]
        /// <summary>
    /// HP update broadcast payload.
    /// </summary>
    public sealed class HpUpdatePayload
    {
        public int hostHp;
        public int clientHp;
    }

    [Serializable]
        /// <summary>
    /// Match result broadcast payload.
    /// </summary>
    public sealed class MatchResultPayload
    {
        public string winnerRole;
    }

    [Serializable]
        /// <summary>
    /// Shared spatial anchor UUID payload.
    /// </summary>
    public sealed class SharedAnchorPayload
    {
        public string uuid;
    }

    [Serializable]
        /// <summary>
    /// Calibration readiness payload.
    /// </summary>
    public sealed class CalibrationReadyPayload
    {
        public bool ready;
        public bool hasPose;
        public bool isLocked;
        public float stability01;
    }

    [Serializable]
        /// <summary>
    /// Step-based remote alignment confirmation and offset synchronization payload.
    /// </summary>
    public sealed class RemoteAlignmentPayload
    {
        public Vector3 position;
        public Quaternion rotation;
        public string senderRole;
        public string stage;
        public bool confirmed;
    }

    [Serializable]
        /// <summary>
    /// Rematch confirmation payload.
    /// </summary>
    public sealed class RematchReadyPayload
    {
        public bool ready;
    }

    [Serializable]
        /// <summary>
    /// Spectator vote interaction payload.
    /// </summary>
    public sealed class SpectatorVotePayload
    {
        public string targetRole;
    }

    [Serializable]
        /// <summary>
    /// Wall obstacle spawn request payload.
    /// </summary>
    public sealed class ObstacleSpawnRequestPayload
    {
        public string anchorType;
        public Vector3 localOffset;
        public float yawOffset;
    }

    [Serializable]
        /// <summary>
    /// Authoritative wall obstacle state payload.
    /// </summary>
    public sealed class ObstacleStatePayload
    {
        public int obstacleId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;
        public float currentHp;
        public float maxHp;
        public bool active;
    }

    [Serializable]
    public struct PoseData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

        /// <summary>
    /// Constants for all network message types.
    /// </summary>
    public static class LanMessageTypes
    {
        public const string Hello = "HELLO";
        public const string HelloAck = "HELLO_ACK";
        public const string Pose = "POSE";
        public const string StartCalibration = "START_CALIBRATION";
        public const string WorldRootSync = "WORLD_ROOT_SYNC";
        public const string Shoot = "SHOOT";
        public const string Shield = "SHIELD";
        public const string HpUpdate = "HP_UPDATE";
        public const string MatchResult = "MATCH_RESULT";
        public const string SharedAnchor = "SHARED_ANCHOR";
        public const string StartPlaying = "START_PLAYING";
        public const string CalibrationReady = "CALIBRATION_READY";
        public const string RemoteAlignment = "REMOTE_ALIGNMENT";
        public const string RematchReady = "REMATCH_READY";
        public const string SpectatorVote = "SPECTATOR_VOTE";
        public const string ObstacleSpawnRequest = "OBSTACLE_SPAWN_REQUEST";
        public const string ObstacleState = "OBSTACLE_STATE";
    }
}




