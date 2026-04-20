using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "CivilianIdleState", menuName = "Main/FSM/Civilian States/Idle State")]
    public class CivilianIdleState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Initialize idle timer using IdleSecondsAfterSafe property
                civilian.StateTimer = civilian.IdleSecondsAfterSafe;
                civilian.SetCurrentMaxSpeed(civilian.WalkSpeed);

                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Entered Idle State - Will idle for {civilian.IdleSecondsAfterSafe} seconds");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                // Countdown the idle timer
                civilian.StateTimer -= Time.deltaTime;

                // Check for player detection - FSM conditions will handle transitions
                // but we still need to perform the behavior

                // Perform gentle drift movement (idle wandering)
                PerformIdleDrift(civilian);

                // Timer reset for continuous idle if no transitions occur
                if (civilian.StateTimer <= 0f)
                {
                    civilian.StateTimer = civilian.IdleSecondsAfterSafe;
                }
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Exited Idle State");
            }
        }

        private void PerformIdleDrift(Civilian civilian)
        {
            // Gentle drift movement (optional idle wandering)
            Vector3 driftDirection = new Vector3(
                Mathf.Sin(Time.time * 0.3f) * 0.5f,
                0f,
                Mathf.Cos(Time.time * 0.2f) * 0.5f
            );

            // Apply gentle steering through the movement funnel
            Vector3 steering = Steering.Seek(
                civilian.transform.position,
                civilian.transform.position + driftDirection,
                civilian.CurrentVelocity,
                civilian.WalkSpeed
            );

            civilian.ApplySteering(steering * 0.3f); // Very gentle movement
        }
    }
}