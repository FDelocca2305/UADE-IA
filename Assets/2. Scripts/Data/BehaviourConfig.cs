using UnityEngine;

namespace Game.AI.Flocking
{
    /// <summary>
    /// Base configuration for all flocking behaviours.
    /// ScriptableObjects contain ONLY data, no logic.
    /// </summary>
    public abstract class FlockBehaviourConfig : ScriptableObject
    {
        [Tooltip("Weight/influence of this behaviour")]
        [Range(0f, 5f)]
        public float weight = 1f;

        [Tooltip("Is this behaviour enabled")]
        public bool enabled = true;
    }

    // ==================== SEPARATION ====================

    /// <summary>
    /// Configuration for separation behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "SeparationConfig", menuName = "Flocking/Behaviours/Separation", order = 0)]
    public class SeparationConfig : FlockBehaviourConfig
    {
        [Header("Separation Settings")]
        [Tooltip("Minimum distance to maintain from neighbors")]
        [Min(0.1f)]
        public float minDistance = 1f;

        [Tooltip("Strength multiplier for separation force")]
        [Range(0f, 5f)]
        public float strength = 1.5f;
    }

    // ==================== COHESION ====================

    /// <summary>
    /// Configuration for cohesion behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "CohesionConfig", menuName = "Flocking/Behaviours/Cohesion", order = 1)]
    public class CohesionConfig : FlockBehaviourConfig
    {
        [Header("Cohesion Settings")]
        [Tooltip("Strength multiplier for cohesion force")]
        [Range(0f, 5f)]
        public float strength = 1f;
    }

    // ==================== ALIGNMENT ====================

    /// <summary>
    /// Configuration for alignment behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "AlignmentConfig", menuName = "Flocking/Behaviours/Alignment", order = 2)]
    public class AlignmentConfig : FlockBehaviourConfig
    {
        [Header("Alignment Settings")]
        [Tooltip("Strength multiplier for alignment force")]
        [Range(0f, 5f)]
        public float strength = 1f;
    }
}
