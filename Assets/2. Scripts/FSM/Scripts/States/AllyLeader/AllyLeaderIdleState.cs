using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderIdleState", menuName = "Main/FSM/AllyLeader States/Idle")]
    public class AllyLeaderIdleState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                MyLogger.LogDebug($"AllyLeader {leader.name}: Entró a Idle");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                leader.ShareIntel();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                MyLogger.LogDebug($"AllyLeader {leader.name}: Salió de Idle");
            }
        }
    }
}
