using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyLeaderHoldPerimeterState", menuName = "Main/FSM/AllyLeader States/Hold Perimeter")]
    public class AllyLeaderHoldPerimeterState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer = 0f;

                Vector3 center = leader.GetPlayerToFollow() != null
                    ? leader.GetPlayerToFollow().position
                    : leader.transform.position;

                leader.IssueHoldPerimeter(center);
                MyLogger.LogInfo($"[AllyLeader] HoldPerimeter -> center {center}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
                if (leader.GetPlayerToFollow() != null)
                {
                    leader.FollowPlayer();
                }
                leader.ShareIntel();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is AllyLeader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogInfo("[AllyLeader] HoldPerimeter exit - cleared overrides");
            }
        }
    }
}
