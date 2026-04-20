using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardCallReinforcementsState", menuName = "Main/FSM/Guard States/Call Reinforcements State")]
    public class GuardCallReinforcementsState : State
    {
        [SerializeField] private float callDuration = 1.5f;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StateTimer = 0f;
                // Asegura última posición de player antes de compartir
                if (guard.LastKnownPlayerPosition == Vector3.zero && guard.GetTargetTransform() != null)
                {
                    guard.LastKnownPlayerPosition = guard.GetTargetTransform().position;
                }

                ShareInfo(guard);
                guard.DeploySmoke();
                MyLogger.LogInfo($"[ReinforceDebug] Guard {guard.name} requesting reinforcements at {guard.transform.position}, lastPlayer {guard.LastKnownPlayerPosition}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StateTimer += Time.deltaTime;

                // Reduce movement / stay put while calling
                guard.ApplySteering(Vector3.zero);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogInfo($"[ReinforceDebug] Guard {guard.name} finished call");
            }
        }

        private void ShareInfo(Guard guard)
        {
            if (guard.BlackboardService == null) return;

            guard.BlackboardService.SetValue("Reinforce_TargetPosition", guard.transform.position);
            guard.BlackboardService.SetValue("Reinforce_LastKnownPlayerPos", guard.LastKnownPlayerPosition);
            guard.BlackboardService.SetValue("Reinforce_RequestTime", Time.time);
        }

        public float CallDuration => callDuration;
    }
}
