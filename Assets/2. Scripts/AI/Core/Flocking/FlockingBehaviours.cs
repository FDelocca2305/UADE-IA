using System.Collections.Generic;
using Game.AI.Flocking;
using Game.AI.Steering;
using UnityEngine;

namespace FlockingSystem
{
    // ==================== SEPARATION ====================

    /// <summary>
    /// Separation logic: Steer away from neighbors to avoid crowding.
    /// Pure static logic - no state, fully testeable.
    /// </summary>
    public static class SeparationBehaviour
    {
        public static Vector3 Calculate(
            FlockingEntity entity,
            List<NeighborData> neighbors,
            SeparationConfig config,
            FlockingProfile profile,
            Vector3 currentVelocity)
        {
            if (neighbors.Count == 0 || !config.enabled)
                return Vector3.zero;

            Vector3 separationForce = Vector3.zero;
            float sqrMinDistance = config.minDistance * config.minDistance;

            foreach (var neighbor in neighbors)
            {
                if (neighbor.sqrDistance > sqrMinDistance || neighbor.sqrDistance < 0.001f)
                    continue;

                Vector3 awayDirection = -neighbor.directionToNeighbor.normalized;
                float strength = (1f / neighbor.distance) * config.strength;
                separationForce += awayDirection * strength;
            }

            if (separationForce.sqrMagnitude < 0.001f)
                return Vector3.zero;

            // Use static Steering.Seek for consistency
            Vector3 targetPos = entity.transform.position + separationForce.normalized;
            return Steering.Seek(entity.transform.position, targetPos, currentVelocity, profile.maxSpeed);
        }
    }

    // ==================== COHESION ====================

    /// <summary>
    /// Cohesion logic: Steer toward the average position of neighbors.
    /// Pure static logic - no state, fully testeable.
    /// </summary>
    public static class CohesionBehaviour
    {
        public static Vector3 Calculate(
            FlockingEntity entity,
            List<NeighborData> neighbors,
            CohesionConfig config,
            FlockingProfile profile,
            Vector3 currentVelocity)
        {
            if (neighbors.Count == 0 || !config.enabled)
                return Vector3.zero;

            Vector3 centerOfMass = Vector3.zero;

            foreach (var neighbor in neighbors)
            {
                centerOfMass += neighbor.position;
            }

            centerOfMass /= neighbors.Count;

            // Use static Steering.Seek
            Vector3 force = Steering.Seek(entity.transform.position, centerOfMass, currentVelocity, profile.maxSpeed);
            return force * config.strength;
        }
    }

    // ==================== ALIGNMENT ====================

    /// <summary>
    /// Alignment logic: Steer toward the average heading of neighbors.
    /// Pure static logic - no state, fully testeable.
    /// </summary>
    public static class AlignmentBehaviour
    {
        public static Vector3 Calculate(
            FlockingEntity entity,
            List<NeighborData> neighbors,
            AlignmentConfig config,
            FlockingProfile profile,
            Vector3 currentVelocity)
        {
            if (neighbors.Count == 0 || !config.enabled)
                return Vector3.zero;

            Vector3 averageVelocity = Vector3.zero;

            foreach (var neighbor in neighbors)
            {
                averageVelocity += neighbor.velocity;
            }

            averageVelocity /= neighbors.Count;

            if (averageVelocity.sqrMagnitude < 0.001f)
                return Vector3.zero;

            // Use static Steering.Seek with predicted position
            Vector3 targetPos = entity.transform.position + averageVelocity.normalized;
            Vector3 force = Steering.Seek(entity.transform.position, targetPos, currentVelocity, profile.maxSpeed);
            return force * config.strength;
        }
    }
}
