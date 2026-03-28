// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines configurable spectator support parameters.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Project/Spectator Support Config", fileName = "SpectatorSupportConfig")]
        /// <summary>
    /// Spectator mode configuration parameters.
    /// </summary>
    public sealed class SpectatorSupportConfig : ScriptableObject
    {
        [Header("Voting Heal")]
        [Min(1)] public int healAmount = 10;
        [Min(0.1f)] public float voteCooldown = 3f;

        [Header("Barrage")]
        public string barrageWordA = "COOL";
        public string barrageWordB = "GOOD GAME";
        public string barrageWordC = "NICE SHOT";
        [Min(0.5f)] public float barrageDuration = 2.4f;
        [Min(0.1f)] public float barrageSpeed = 0.42f;

        [Header("Local Audio")]
        public AudioClip cheerClip;
        public AudioClip applauseClip;
        [Range(0f, 1f)] public float audioVolume = 0.9f;

        [Header("Wall Obstacle")]
        [Min(1)] public int wallMaxHp = 100;
        [Min(0.1f)] public float wallDecayPerSecond = 5f;
        [Min(1)] public int wallShotDamage = 10;
        [Min(0.1f)] public float wallPlacementDistance = 1.4f;
        [Min(0.1f)] public float wallSpawnCooldown = 2f;
        [Min(1)] public int wallMaxActiveCount = 2;
        public Vector3 wallSize = new Vector3(1.6f, 1.35f, 0.12f);
    }
}




