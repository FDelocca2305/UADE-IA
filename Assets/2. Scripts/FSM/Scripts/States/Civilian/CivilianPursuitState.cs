using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "CivilianPursuitState", menuName = "Main/FSM/Civilian States/Pursuit State")]
    public class CivilianPursuitState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Reset lose sight timer when entering pursuit
                civilian.PursuitLoseSightTimer = 0f;
                civilian.SetCurrentMaxSpeed(civilian.PursueSpeed);

                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Entered Pursuit State - Chasing at speed {civilian.PursueSpeed:F1}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Perform pursuit movement only - Decision Tree handles transitions
                PerformPursuitMovement(civilian);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Reset timer when exiting
                civilian.PursuitLoseSightTimer = 0f;
                
                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Exited Pursuit State - LoseSight timer: {civilian.PursuitLoseSightTimer:F2}s");
            }
        }


        private void PerformPursuitMovement(Civilian civilian)
        {
            if (civilian.Player == null) return;

            Vector3 civilianPos = civilian.transform.position;
            Vector3 playerPos = civilian.Player.position;
            
            // Get player velocity if available
            Vector3 playerVelocity = Vector3.zero;
            var playerRb = civilian.Player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerVelocity = playerRb.linearVelocity;
            }

            // Use Steering.Pursuit for predictive chasing
            Vector3 steering = Steering.Pursuit(
                civilianPos,
                civilian.CurrentVelocity,
                playerPos,
                playerVelocity,
                civilian.PursueSpeed
            );

            // Apply through the movement funnel (ApplySteering -> ObstacleAvoidance.GetDir2 -> move)
            civilian.ApplySteering(steering);
        }
    }
}