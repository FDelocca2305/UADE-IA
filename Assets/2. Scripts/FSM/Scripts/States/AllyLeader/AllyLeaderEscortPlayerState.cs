using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderEscortPlayerState", menuName = "Main/FSM/AllyLeader States/Escort Player")]
    public class AllyLeaderEscortPlayerState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                leader.IssueEscortPlayerCommand();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Entro a EscortPlayer");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                leader.FollowPlayer(); // Acompaña al jugador mientras mantiene escolta
                if (!leader.HasAvailableAllies())
                {
                    leader.DiscoverNearbyAllies();
                }
                leader.ShareIntel();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Salio de EscortPlayer");
            }
        }
    }
}
