using UnityEngine;

namespace Game.AI.Steering
{
    /// <summary>
    /// Reynolds-style steering behaviors for AI movement.
    /// Pure functions with minimal GC allocation and XZ-plane math.
    /// </summary>
    public static class Steering
    {
        /// <summary>
        /// Seek behavior - steer towards a target position
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="target">Target position to seek</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <returns>Steering force vector</returns>
        public static Vector3 Seek(Vector3 pos, Vector3 target, Vector3 vel, float maxSpeed)
        {
            // Calculate desired velocity towards target
            Vector3 desired = target - pos;
            desired.y = 0f; // Keep in XZ plane

            // Normalize and scale to max speed
            if (desired.sqrMagnitude > 0.001f)
            {
                desired = desired.normalized * maxSpeed;
            }

            // Steering force = desired velocity - current velocity
            Vector3 steering = desired - vel;
            steering.y = 0f; // Ensure XZ plane

            return steering;
        }

        /// <summary>
        /// Flee behavior - steer away from a position
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="from">Position to flee from</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <returns>Steering force vector</returns>
        public static Vector3 Flee(Vector3 pos, Vector3 from, Vector3 vel, float maxSpeed)
        {
            // Calculate desired velocity away from target
            Vector3 desired = pos - from;
            desired.y = 0f; // Keep in XZ plane

            // Normalize and scale to max speed
            if (desired.sqrMagnitude > 0.001f)
            {
                desired = desired.normalized * maxSpeed;
            }

            // Steering force = desired velocity - current velocity
            Vector3 steering = desired - vel;
            steering.y = 0f; // Ensure XZ plane

            return steering;
        }

        /// <summary>
        /// Arrive behavior - seek with deceleration as approaching target
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="target">Target position to arrive at</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <param name="slowingDistance">Distance at which to start slowing down</param>
        /// <returns>Steering force vector</returns>
        public static Vector3 Arrive(Vector3 pos, Vector3 target, Vector3 vel,
                                     float maxSpeed, float slowingDistance)
        {
            // Calculate vector to target
            Vector3 toTarget = target - pos;
            toTarget.y = 0f; // Keep in XZ plane

            float distance = toTarget.magnitude;

            if (distance < 0.001f)
            {
                // Already at target, stop
                return -vel;
            }

            // Calculate desired speed based on distance
            float desiredSpeed;
            if (distance < slowingDistance)
            {
                // Inside slowing radius - decelerate
                desiredSpeed = maxSpeed * (distance / slowingDistance);
            }
            else
            {
                // Outside slowing radius - full speed
                desiredSpeed = maxSpeed;
            }

            // Calculate desired velocity
            Vector3 desired = (toTarget / distance) * desiredSpeed;

            // Steering force = desired velocity - current velocity
            Vector3 steering = desired - vel;
            steering.y = 0f; // Ensure XZ plane

            return steering;
        }

        /// <summary>
        /// Pursuit behavior - seek predicted future position of moving target
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="tgtPos">Target current position</param>
        /// <param name="tgtVel">Target velocity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <param name="maxPrediction">Maximum time to predict ahead</param>
        /// <returns>Steering force vector</returns>
        public static Vector3 Pursuit(Vector3 pos, Vector3 vel, Vector3 tgtPos, Vector3 tgtVel,
                                      float maxSpeed, float maxPrediction = 1.25f)
        {
            // Calculate vector to target
            Vector3 toTarget = tgtPos - pos;
            toTarget.y = 0f; // Keep in XZ plane

            // Calculate relative heading (dot product of normalized velocities)
            Vector3 velNorm = vel;
            Vector3 tgtVelNorm = tgtVel;
            velNorm.y = 0f;
            tgtVelNorm.y = 0f;

            if (velNorm.sqrMagnitude > 0.001f) velNorm.Normalize();
            if (tgtVelNorm.sqrMagnitude > 0.001f) tgtVelNorm.Normalize();

            float relativeHeading = Vector3.Dot(velNorm, tgtVelNorm);

            // If target is ahead and moving away, use seek behavior
            if (Vector3.Dot(toTarget.normalized, velNorm) > 0 && relativeHeading < -0.95f)
            {
                return Seek(pos, tgtPos, vel, maxSpeed);
            }

            // Calculate prediction time based on distance and relative speed
            float distance = toTarget.magnitude;
            float relativeSpeed = vel.magnitude + tgtVel.magnitude;

            float predictionTime;
            if (relativeSpeed <= 0.001f)
            {
                predictionTime = 0f;
            }
            else
            {
                predictionTime = distance / relativeSpeed;
                predictionTime = Mathf.Min(predictionTime, maxPrediction);
            }

            // Predict future target position
            Vector3 futurePos = tgtPos + tgtVel * predictionTime;

            // Seek the predicted position
            return Seek(pos, futurePos, vel, maxSpeed);
        }

        /// <summary>
        /// Evade behavior - flee from predicted future position of pursuer
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="pursuerPos">Pursuer current position</param>
        /// <param name="pursuerVel">Pursuer velocity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <param name="maxPrediction">Maximum time to predict ahead</param>
        /// <returns>Steering force vector</returns>
        public static Vector3 Evade(Vector3 pos, Vector3 vel, Vector3 pursuerPos, Vector3 pursuerVel,
                                    float maxSpeed, float maxPrediction = 1.25f)
        {
            // Calculate vector from pursuer to us
            Vector3 toPursuer = pursuerPos - pos;
            toPursuer.y = 0f; // Keep in XZ plane

            // Calculate prediction time based on distance and pursuer speed
            float distance = toPursuer.magnitude;
            float pursuerSpeed = pursuerVel.magnitude;

            float predictionTime;
            if (pursuerSpeed <= 0.001f)
            {
                predictionTime = 0f;
            }
            else
            {
                predictionTime = distance / pursuerSpeed;
                predictionTime = Mathf.Min(predictionTime, maxPrediction);
            }

            // Predict future pursuer position
            Vector3 futurePursuerPos = pursuerPos + pursuerVel * predictionTime;

            // Flee from the predicted position
            return Flee(pos, futurePursuerPos, vel, maxSpeed);
        }

        /// <summary>
        /// Utility method to clamp steering force magnitude
        /// </summary>
        /// <param name="steering">Steering force to clamp</param>
        /// <param name="maxForce">Maximum force magnitude</param>
        /// <returns>Clamped steering force</returns>
        public static Vector3 ClampMagnitude(Vector3 steering, float maxForce)
        {
            if (steering.sqrMagnitude > maxForce * maxForce)
            {
                steering = steering.normalized * maxForce;
            }
            return steering;
        }

        /// <summary>
        /// Utility method to combine multiple steering forces with weights
        /// </summary>
        /// <param name="forces">Array of steering forces</param>
        /// <param name="weights">Array of weights (must match forces length)</param>
        /// <param name="maxForce">Maximum combined force magnitude</param>
        /// <returns>Combined and clamped steering force</returns>
        public static Vector3 CombineForces(Vector3[] forces, float[] weights, float maxForce)
        {
            Vector3 combined = Vector3.zero;
            int count = Mathf.Min(forces.Length, weights.Length);

            for (int i = 0; i < count; i++)
            {
                combined += forces[i] * weights[i];
            }

            return ClampMagnitude(combined, maxForce);
        }
    }
}