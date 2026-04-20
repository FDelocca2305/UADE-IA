using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyChaseGuardState", menuName = "Main/FSM/Ally States/Chase Guard")]
    public class AllyChaseGuardState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                MyLogger.LogDebug($"Ally {ally.name}: Entrando a estado ChaseGuard");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.ChaseCurrentGuard();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                MyLogger.LogDebug($"Ally {ally.name}: Saliendo de estado ChaseGuard");
            }
        }
    }
}
