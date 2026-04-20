using UnityEngine;

namespace Game.AI.Flocking
{
    /// <summary>
    /// Main configuration profile for flocking behaviour.
    /// ScriptableObject contains only configuration data - no logic.
    /// Create via: Create > Flocking > Flocking Profile
    /// </summary>
    [CreateAssetMenu(fileName = "FlockingProfile", menuName = "Flocking/Flocking Profile", order = 0)]
    public class FlockingProfile : ScriptableObject
    {
        [Header("Behaviour Configurations")]
        [Tooltip("Separation behaviour settings")]
        public SeparationConfig separationConfig;

        [Tooltip("Cohesion behaviour settings")]
        public CohesionConfig cohesionConfig;

        [Tooltip("Alignment behaviour settings")]
        public AlignmentConfig alignmentConfig;

        [Header("Detection")]
        [Tooltip("Maximum distance to detect neighbors")]
        [Min(0.1f)]
        public float detectionRadius = 5f;

        [Header("Movement")]
        [Tooltip("Maximum speed the entity can move")]
        [Min(0.1f)]
        public float maxSpeed = 5f;

        [Tooltip("Maximum force that can be applied per frame")]
        [Min(0.1f)]
        public float maxForce = 3f;

        [Header("Performance")]
        [Tooltip("Maximum number of neighbors to consider (0 = unlimited)")]
        [Min(0)]
        public int maxNeighbors = 20;
    }
}