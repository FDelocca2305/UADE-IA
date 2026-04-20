using Services.MicroServices.BlackboardService;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardSearchState", menuName = "Main/FSM/Guard States/Search State")]
    public class GuardSearchState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StateTimer = 0f;
                MyLogger.LogDebug($"Guard {guard.name}: Entered Search State - Looking for player at {guard.LastKnownPlayerPosition}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                SearchForPlayer(guard);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Exited Search State");
            }
        }

        private void SearchForPlayer(Guard guard)
        {
            // Minimum scope: Get last known position from blackboard for coordination
            Vector3 targetPosition = guard.LastKnownPlayerPosition;
            if (guard.BlackboardService != null)
            {
                Vector3 blackboardPosition = guard.BlackboardService.GetValue<Vector3>(BlackboardKeys.LAST_KNOWN_PLAYER_POSITION);
                if (blackboardPosition != Vector3.zero)
                {
                    targetPosition = blackboardPosition;
                }
            }

            Vector3 direction = (targetPosition - guard.transform.position).normalized;
            direction.y = 0;

            float distanceToLastKnown = Vector3.Distance(guard.transform.position, targetPosition);
            
            if (distanceToLastKnown > 1f && direction.magnitude > 0.1f)
            {
                guard.Move(direction.normalized * guard.PatrolSpeed);
            }
        }
    }
}