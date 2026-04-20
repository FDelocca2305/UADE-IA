using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderAttackTargetState", menuName = "Main/FSM/AllyLeader States/Attack Target")]
    public class AllyLeaderAttackTargetState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                Guard l_target = leader.GetCurrentTarget();
                if (l_target != null)
                {
                    leader.IssueAttackCommandOnTarget(l_target.transform);
                    MyLogger.LogDebug($"AllyLeader {leader.name}: Ordeno atacar a {l_target.name}");
                }
                else
                {
                    MyLogger.LogDebug($"AllyLeader {leader.name}: No tiene objetivo para AttackTarget");
                }
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                Guard l_target = leader.GetCurrentTarget();
                if (l_target != null)
                {
                    leader.IssueAttackCommandOnTarget(l_target.transform);
                }
                leader.ShareIntel();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogDebug($"AllyLeader {leader.name}: Salio de AttackTarget");
            }
        }
    }
}
