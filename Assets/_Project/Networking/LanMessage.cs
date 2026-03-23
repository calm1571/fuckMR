using System;
using UnityEngine;

namespace Project.Networking
{
    [Serializable]
        /// <summary>
    /// 通用网络消息外壳。
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
    /// 头手位姿同步数据。
    /// </summary>
    public sealed class PosePayload
    {
        public PoseData head;
        public PoseData leftHand;
        public PoseData rightHand;
    }

    [Serializable]
        /// <summary>
    /// WorldRoot 同步数据。
    /// </summary>
    public sealed class WorldRootSyncPayload
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
        /// <summary>
    /// 开火消息数据。
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
    /// 护盾开关消息数据。
    /// </summary>
    public sealed class ShieldPayload
    {
        public bool active;
        public float duration;
    }

    [Serializable]
        /// <summary>
    /// HP 广播数据。
    /// </summary>
    public sealed class HpUpdatePayload
    {
        public int hostHp;
        public int clientHp;
    }

    [Serializable]
        /// <summary>
    /// 对局结果广播数据。
    /// </summary>
    public sealed class MatchResultPayload
    {
        public string winnerRole;
    }

    [Serializable]
        /// <summary>
    /// 共享空间锚 UUID 数据。
    /// </summary>
    public sealed class SharedAnchorPayload
    {
        public string uuid;
    }

    [Serializable]
        /// <summary>
    /// 校准就绪状态数据。
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
    /// 分步远端对齐确认与偏移同步数据。
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
    /// 重赛确认数据。
    /// </summary>
    public sealed class RematchReadyPayload
    {
        public bool ready;
    }

    [Serializable]
        /// <summary>
    /// Spectator 投票型交互数据。
    /// </summary>
    public sealed class SpectatorVotePayload
    {
        public string targetRole;
    }

    [Serializable]
        /// <summary>
    /// 障碍墙生成请求数据。
    /// </summary>
    public sealed class ObstacleSpawnRequestPayload
    {
        public string anchorType;
        public Vector3 localOffset;
        public float yawOffset;
    }

    [Serializable]
        /// <summary>
    /// 障碍墙权威状态数据。
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
    /// 所有网络消息类型常量。
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

