using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyAttackGuardState", menuName = "Main/FSM/Ally States/Attack Guard")]
    public class AllyAttackGuardState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                MyLogger.LogDebug($"Ally {ally.name}: Entrando a estado AttackGuard");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.AttackGuard(ally.GetCurrentTarget());
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                MyLogger.LogDebug($"Ally {ally.name}: Saliendo de estado AttackGuard");
            }
        }
    }
}
