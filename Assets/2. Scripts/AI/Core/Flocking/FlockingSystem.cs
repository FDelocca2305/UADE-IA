using System.Collections.Generic;
using Game.AI.Flocking;
using UnityEngine;

namespace FlockingSystem
{
    /// <summary>
    /// Entity that exhibits flocking behaviour based on a configurable profile.
    /// Registers with IFlockingService and calculates forces from nearby neighbors.
    /// Uses static Steering class for all movement calculations.
    /// MODIFIED: Now only calculates and returns forces without applying movement.
    /// Movement is controlled by host entity (e.g., Civilian).
    /// </summary>
    public class FlockingEntity : MonoBehaviour
    {
        [Header("Flocking Configuration")]
        [Tooltip("Profile defining behaviours and parameters")]
        [SerializeField] private FlockingProfile profile;

        [Header("References")]
        [Tooltip("Flocking service resolved via ServiceLocator at runtime")]
        private Services.MicroServices.FlockingService.IFlockingService flockingService;

        private Civilian civilian;

        private Guard guard;

        [Header("Performance")]
        [Tooltip("Update interval in frames (2 = every other frame)")]
        [SerializeField] private int updateInterval = 2;

        private List<NeighborData> cachedNeighbors = new List<NeighborData>();
        private Vector3 cachedFlockingForce = Vector3.zero;
        private int updateOffset;
        private float maxSpeed;
        private float maxForce;

        public FlockingProfile Profile => profile;
        public Vector3 CachedFlockingForce => cachedFlockingForce;

        /// <summary>
        /// Velocity property - reads from Civilian/Guard if available, otherwise returns zero
        /// Used by NeighborData to cache neighbor velocities
        /// </summary>
        public Vector3 Velocity
        {
            get
            {
                if (civilian != null)
                {
                    return civilian.CurrentVelocity;
                }
                if (guard != null)
                {
                    return guard.CurrentVelocity;
                }
                return Vector3.zero;
            }
        }

        private void Awake()
        {
            // Random offset for load balancing across frames
            updateOffset = Random.Range(0, updateInterval);

            // Auto-find Civilian/Guard component if not assigned
            if (civilian == null)
            {
                civilian = GetComponent<Civilian>();
            }
            if (guard == null)
            {
                guard = GetComponent<Guard>();
            }

            // Initialize speed and force from profile
            if (profile != null)
            {
                maxSpeed = profile.maxSpeed;
                maxForce = profile.maxForce;
            }
        }

        private void Start()
        {
            // Resolve service and register entity
            flockingService = Services.ServiceLocator.Get<Services.MicroServices.FlockingService.IFlockingService>();

            if (flockingService != null)
            {
                flockingService.RegisterEntity(this);
            }
            else
            {
                MyLogger.LogError($"IFlockingService not available for {gameObject.name}");
            }
        }

        private void Update()
        {
            if (profile == null)
                return;

            // Frame skipping for performance (update every N frames with offset)
            if ((Time.frameCount + updateOffset) % updateInterval != 0)
                return;

            // Check if host entity wants flocking active
            if (!ShouldCalculateFlocking())
            {
                cachedFlockingForce = Vector3.zero;
                return;
            }

            UpdateNeighbors();
            cachedFlockingForce = CalculateFlockingForce();
        }

        private void OnDestroy()
        {
            if (flockingService != null)
            {
                flockingService.UnregisterEntity(this);
            }
        }

        /// <summary>
        /// Check if flocking should be calculated this frame.
        /// Delegates to Civilian/Guard ShouldFlock() if available.
        /// </summary>
        private bool ShouldCalculateFlocking()
        {
            // If no host reference, always calculate (standalone mode)
            if (civilian == null && guard == null)
                return true;

            // Delegate to host to check if it's in appropriate state
            if (guard != null)
                return guard.ShouldFlock();
            return true;
        }

        private void UpdateNeighbors()
        {
            cachedNeighbors.Clear();

            if (flockingService == null)
                return;

            List<FlockingEntity> nearbyEntities = flockingService.GetNeighbors(this, profile.detectionRadius);

            int maxNeighbors = profile.maxNeighbors > 0 ? profile.maxNeighbors : nearbyEntities.Count;
            int count = Mathf.Min(nearbyEntities.Count, maxNeighbors);

            for (int i = 0; i < count; i++)
            {
                cachedNeighbors.Add(new NeighborData(this, nearbyEntities[i]));
            }
        }

        /// <summary>
        /// Calculate combined flocking forces from all enabled behaviors.
        /// Returns force vector without applying it (host entity applies it).
        /// </summary>
        private Vector3 CalculateFlockingForce()
        {
            if (cachedNeighbors.Count == 0)
                return Vector3.zero;

            Vector3 totalForce = Vector3.zero;

            // Get current velocity from host if available
            Vector3 currentVelocity = Vector3.zero;
            if (civilian != null)
            {
                currentVelocity = civilian.CurrentVelocity;
            }
            else if (guard != null)
            {
                currentVelocity = guard.CurrentVelocity;
            }

            // Separation
            if (profile.separationConfig != null && profile.separationConfig.enabled)
            {
                Vector3 separationForce = SeparationBehaviour.Calculate(
                    this, cachedNeighbors, profile.separationConfig, profile, currentVelocity);
                totalForce += separationForce * profile.separationConfig.weight;
            }

            // Cohesion
            if (profile.cohesionConfig != null && profile.cohesionConfig.enabled)
            {
                Vector3 cohesionForce = CohesionBehaviour.Calculate(
                    this, cachedNeighbors, profile.cohesionConfig, profile, currentVelocity);
                totalForce += cohesionForce * profile.cohesionConfig.weight;
            }

            // Alignment
            if (profile.alignmentConfig != null && profile.alignmentConfig.enabled)
            {
                Vector3 alignmentForce = AlignmentBehaviour.Calculate(
                    this, cachedNeighbors, profile.alignmentConfig, profile, currentVelocity);
                totalForce += alignmentForce * profile.alignmentConfig.weight;
            }

            // Keep in XZ plane
            totalForce.y = 0f;

            return totalForce;
        }

        /// <summary>
        /// Public API: Get the most recently calculated flocking force.
        /// Host entity (e.g., Civilian) calls this to integrate flocking into movement.
        /// </summary>
        /// <returns>Cached flocking force vector</returns>
        public Vector3 GetFlockingForce()
        {
            return cachedFlockingForce;
        }

        /// <summary>
        /// Change flocking profile at runtime
        /// </summary>
        public void SetProfile(FlockingProfile newProfile)
        {
            profile = newProfile;

            if (profile != null)
            {
                maxSpeed = profile.maxSpeed;
                maxForce = profile.maxForce;
            }
        }
    }
}
