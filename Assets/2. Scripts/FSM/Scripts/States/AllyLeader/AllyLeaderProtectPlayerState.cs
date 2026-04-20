using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderProtectPlayerState", menuName = "Main/FSM/AllyLeader States/Protect Player")]
    public class AllyLeaderProtectPlayerState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                leader.IssueProtectPlayerCommand();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Ordenó proteger al jugador");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                leader.FollowPlayer(); // Mantener al líder cerca del jugador mientras protege
                leader.ShareIntel();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Salió de ProtectPlayer");
            }
        }
    }
}
