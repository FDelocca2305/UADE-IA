using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderRallyAlliesState", menuName = "Main/FSM/AllyLeader States/Rally Allies")]
    public class AllyLeaderRallyAlliesState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                leader.ClearAllOverrides();
                leader.DiscoverNearbyAllies();
                leader.IssueRegroupOnLeader();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Entro a RallyAllies");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                leader.FollowPlayer(); // Mantener al líder alineado al jugador mientras reagrupa
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
                MyLogger.LogDebug($"AllyLeader {leader.name}: Salio de RallyAllies");
            }
        }
    }
}
