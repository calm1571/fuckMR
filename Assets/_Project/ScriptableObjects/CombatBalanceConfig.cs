// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines configurable combat balance parameters.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Project/Combat Balance Config", fileName = "CombatBalanceConfig")]
        /// <summary>
    /// Combat balance configuration.
    /// </summary>
    public sealed class CombatBalanceConfig : ScriptableObject
    {
        [Header("Core")]
        [Min(1)] public int hp = 100;
        [Min(1)] public int damage = 10;

        [Header("Projectile")]
        [Min(0.1f)] public float projectileSpeed = 5f;
        [Min(0.01f)] public float projectileRadius = 0.033f;
        [Min(0.1f)] public float shootCooldown = 0.5f;

        [Header("Shield")]
        [Min(0.1f)] public float shieldDuration = 1f;
        [Min(0.1f)] public float shieldCooldown = 3f;
    }
}




