using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardAttackState", menuName = "Main/FSM/Guard States/Attack State")]
    public class GuardAttackState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Entered Attack State - Engaging player");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                AttackPlayer(guard);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Exited Attack State");
            }
        }

        private void AttackPlayer(Guard guard)
        {
            Transform target = guard.GetTargetTransform();
            if (target == null) return;

            float distanceToPlayer = Vector3.Distance(guard.transform.position, target.position);

            // If too far from attackrange, pursue the player using steering
            if (distanceToPlayer > guard.AttackRange)
            {
                // Use Pursuit behavior for intelligent chasing
                Vector3 playerVelocity = Vector3.zero;
                var playerRb = target.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerVelocity = playerRb.linearVelocity;
                }

                Vector3 steering = Steering.Pursuit(
                    guard.transform.position,
                    guard.CurrentVelocity,
                    target.position,
                    playerVelocity,
                    guard.ChaseSpeed
                );

                guard.ApplySteering(steering);
            }
            else
            {
                // Within attack range - apply braking force to stop
                // Using Arrive behavior with current position as target causes deceleration
                Vector3 currentVel = guard.CurrentVelocity;
                if (currentVel.sqrMagnitude > 0.01f)
                {
                    // Apply braking force opposite to current velocity
                    Vector3 brakingForce = -currentVel * guard.MaxForce;
                    guard.ApplySteering(brakingForce);
                }

                // Face the target
                Vector3 direction = (target.position - guard.transform.position).normalized;
                direction.y = 0;

                if (direction.magnitude > 0.1f)
                {
                    guard.transform.rotation = Quaternion.LookRotation(direction);
                }

                // Attack
                guard.Shoot(direction);
            }
        }
    }
}