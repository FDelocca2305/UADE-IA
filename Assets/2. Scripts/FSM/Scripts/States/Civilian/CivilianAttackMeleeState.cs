using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "CivilianAttackMeleeState", menuName = "Main/FSM/Civilian States/Attack Melee State")]
    public class CivilianAttackMeleeState : State
    {
        private float attackTimer = 0f;
        private bool didHit = false;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Initialize attack state
                attackTimer = 0f;
                didHit = false;
                civilian.SetCurrentMaxSpeed(civilian.PursueSpeed);

                // Change color to red for visual feedback
                civilian.SetAttackColor();

                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Entered Attack Melee State - Starting windup phase");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Check if we lost sight of player
                if (!civilian.HasLoS())
                {
                    if (civilian.EnableDebugLogs)
                        MyLogger.LogInfo($"Civilian {civilian.name}: Lost sight during attack, aborting");
                    
                    // Will transition to Idle
                    return;
                }

                // Maintain contact with player using Arrive steering
                MaintainMeleeContact(civilian);

                // Update attack timer
                attackTimer += Time.deltaTime;

                // Attack state machine: windup → hit → recover
                HandleAttackPhases(civilian);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Restore original color
                civilian.RestoreOriginalColor();

                // Reset attack state
                attackTimer = 0f;
                didHit = false;

                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Exited Attack Melee State - Restoring original color");
            }
        }

        private void MaintainMeleeContact(Civilian civilian)
        {
            if (civilian.Player == null) return;

            Vector3 civilianPos = civilian.transform.position;
            Vector3 playerPos = civilian.Player.position;

            // Use Arrive to maintain contact without overshooting
            Vector3 steering = Steering.Arrive(
                civilianPos,
                playerPos,
                civilian.CurrentVelocity,
                civilian.PursueSpeed,
                civilian.MeleeRange // Use melee range as slowing distance
            );

            // Apply through the movement funnel (ApplySteering -> ObstacleAvoidance.GetDir2 -> move)
            civilian.ApplySteering(steering);
        }

        private void HandleAttackPhases(Civilian civilian)
        {
            float totalAttackTime = civilian.AttackWindup + civilian.AttackHitWin + civilian.AttackRecover;
            float windupEnd = civilian.AttackWindup;
            float hitWindowEnd = windupEnd + civilian.AttackHitWin;

            if (civilian.EnableDebugLogs)
            {
                // Log attack phases
                if (attackTimer <= windupEnd)
                {
                    // Windup phase
                    if (Mathf.FloorToInt(attackTimer * 10) != Mathf.FloorToInt((attackTimer - Time.deltaTime) * 10))
                    {
                        MyLogger.LogInfo($"Civilian {civilian.name}: Windup phase - {attackTimer:F2}s / {windupEnd:F2}s");
                    }
                }
                else if (attackTimer <= hitWindowEnd && !didHit)
                {
                    // Hit window
                    MyLogger.LogInfo($"Civilian {civilian.name}: Hit window active - {(attackTimer - windupEnd):F2}s / {civilian.AttackHitWin:F2}s");
                }
            }

            // Hit window: try to deal damage
            if (!didHit && attackTimer >= windupEnd && attackTimer <= hitWindowEnd)
            {
                float distanceToPlayer = civilian.GetDistanceToPlayer();
                
                // Check if player is still in range for hit
                if (distanceToPlayer <= civilian.MeleeRange + 0.05f) // Small tolerance
                {
                    civilian.DealMeleeAttack();
                    didHit = true;

                    if (civilian.EnableDebugLogs)
                        MyLogger.LogInfo($"Civilian {civilian.name}: HIT! Dealt {civilian.MeleeDamage} damage (distance: {distanceToPlayer:F2})");
                }
            }

            // Check if attack cycle is complete
            if (attackTimer >= totalAttackTime)
            {
                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Attack cycle complete after {attackTimer:F2}s");
                
                // Will transition to Idle via AttackCompleteCondition
            }
        }
    }
}