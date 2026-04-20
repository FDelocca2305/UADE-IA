using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardPatrolState", menuName = "Main/FSM/Guard States/Patrol State")]
    public class GuardPatrolState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                InitializePatrolling(guard);
                MyLogger.LogDebug($"Guard {guard.name}: Entered Patrol State - Starting patrol loop {guard.CurrentPatrolLoops}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                PerformPatrolMovement(guard);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Exited Patrol State - Completed {guard.CurrentPatrolLoops} loops");
            }
        }

        private void InitializePatrolling(Guard guard)
        {
            if (guard.PatrolPoints == null || guard.PatrolPoints.Length == 0) return;

            // Reset reached flag
            guard.HasReachedCurrentPatrolPoint = false;

            // Ensure valid patrol index
            if (guard.CurrentPatrolIndex < 0 || guard.CurrentPatrolIndex >= guard.PatrolPoints.Length)
            {
                guard.CurrentPatrolIndex = 0;
                guard.PatrolDirection = true;
            }
        }

        private void PerformPatrolMovement(Guard guard)
        {
            if (guard.PatrolPoints == null || guard.PatrolPoints.Length == 0) return;

            var currentPatrolPoint = guard.PatrolPoints[guard.CurrentPatrolIndex];
            if (currentPatrolPoint == null) return;

            // Use Steering.Arrive for smooth movement with deceleration
            Vector3 steering = Steering.Arrive(
                guard.transform.position,
                currentPatrolPoint.position,
                guard.CurrentVelocity,
                guard.PatrolSpeed,
                guard.SlowingDistance
            );

            guard.ApplySteering(steering);

            // Check if reached current patrol point
            float distanceToTarget = Vector3.Distance(guard.transform.position, currentPatrolPoint.position);
            if (distanceToTarget <= 1.5f) // Arrival threshold
            {
                if (!guard.HasReachedCurrentPatrolPoint)
                {
                    guard.HasReachedCurrentPatrolPoint = true;
                    MoveToNextPatrolPoint(guard);
                }
            }
        }

        private void MoveToNextPatrolPoint(Guard guard)
        {
            if (guard.PatrolPoints.Length <= 1) return;

            int lastIndex = guard.PatrolPoints.Length - 1;

            // Ping-pong logic: 0..N..0
            if (guard.PatrolDirection) // Moving forward (0->N)
            {
                if (guard.CurrentPatrolIndex >= lastIndex)
                {
                    // Reached end, reverse direction
                    guard.PatrolDirection = false;
                    guard.CurrentPatrolIndex = lastIndex - 1;
                    guard.CurrentPatrolLoops++;
                }
                else
                {
                    guard.CurrentPatrolIndex++;
                }
            }
            else // Moving backward (N->0)
            {
                if (guard.CurrentPatrolIndex <= 0)
                {
                    // Reached start, reverse direction
                    guard.PatrolDirection = true;
                    guard.CurrentPatrolIndex = 1;
                    guard.CurrentPatrolLoops++;
                }
                else
                {
                    guard.CurrentPatrolIndex--;
                }
            }

            guard.HasReachedCurrentPatrolPoint = false;

            MyLogger.LogDebug($"Guard {guard.name}: Moving to patrol point {guard.CurrentPatrolIndex}, " +
                          $"Direction: {(guard.PatrolDirection ? "Forward" : "Backward")}, " +
                          $"Loops: {guard.CurrentPatrolLoops}");
        }
    }
}