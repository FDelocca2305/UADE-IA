using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderCoverFireState", menuName = "Main/FSM/AllyLeader States/Cover Fire")]
    public class AllyLeaderCoverFireState : State
    {
        private float m_nextBurstTime;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;
                m_nextBurstTime = 0f;

                Guard target = leader.GetCurrentTarget();
                if (target != null)
                {
                    leader.IssueAttackCommandOnTarget(target.transform);
                }

                MyLogger.LogInfo($"[AllyLeader] CoverFire -> target {(target != null ? target.name : "none")}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not AllyLeader leader) return;

            leader.LeaderStateTimer += Time.deltaTime;

            Guard target = leader.GetCurrentTarget();
            if (target != null)
            {
                leader.IssueAttackCommandOnTarget(target.transform);
                FireBurst(leader, target.transform.position);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogInfo("[AllyLeader] CoverFire exit - cleared overrides");
            }
        }

        private void FireBurst(AllyLeader leader, Vector3 targetPos)
        {
            if (Time.time < m_nextBurstTime) return;

            Vector3 dir = targetPos - leader.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f) return;

            leader.FaceDirectionTowards(dir.normalized);
            leader.Shoot(dir.normalized);

            m_nextBurstTime = Time.time + leader.CoverFireFireRate;
        }
    }
}
