using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardChaseState", menuName = "Main/FSM/Guard States/Chase State")]
    public class GuardChaseState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Entered Chase State");
                if (guard.GetTargetTransform() != null)
                {
                    guard.LastKnownPlayerPosition = guard.GetTargetTransform().position;
                }
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                ChasePlayer(guard);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogDebug($"Guard {guard.name}: Exited Chase State");
            }
        }

        private void ChasePlayer(Guard guard)
        {
            Transform target = guard.GetTargetTransform();
            if (target == null) return;

            Vector3 direction = (target.position - guard.transform.position).normalized;
            direction.y = 0;

            if (direction.magnitude > 0.1f)
            {
                guard.Move(direction.normalized * guard.ChaseSpeed);
                guard.LastKnownPlayerPosition = target.position;
            }
        }
    }
}