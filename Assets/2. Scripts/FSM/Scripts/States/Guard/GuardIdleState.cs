using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardIdleState", menuName = "Main/FSM/Guard States/Idle State")]
    public class GuardIdleState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                // Initialize idle timer using IdleSeconds property
                guard.StateTimer = guard.IdleSeconds;

                // Reset patrol loops for next patrol cycle
                guard.CurrentPatrolLoops = 0;

                MyLogger.LogDebug($"Guard {guard.name}: Entered Idle State - Will idle for {guard.IdleSeconds} seconds");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                // Countdown the idle timer
                guard.StateTimer -= Time.deltaTime;

                // Apply brake force to gradually stop movement
                Vector3 brakeForce = -guard.CurrentVelocity * 5f; // Brake force proportional to velocity
                guard.ApplySteering(brakeForce);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Exited Idle State - Returning to patrol");
            }
        }
    }
}