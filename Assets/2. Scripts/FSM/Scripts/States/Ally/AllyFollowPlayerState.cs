using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyFollowPlayerState", menuName = "Main/FSM/Ally States/Follow Player")]
    public class AllyFollowPlayerState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                MyLogger.LogDebug($"Ally {ally.name}: Entrando a estado FollowPlayer");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.FollowPlayer();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                MyLogger.LogDebug($"Ally {ally.name}: Saliendo de estado FollowPlayer");
            }
        }
    }
}
