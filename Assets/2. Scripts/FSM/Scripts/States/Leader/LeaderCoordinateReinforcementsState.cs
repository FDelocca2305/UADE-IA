using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "LeaderCoordinateReinforcementsState", menuName = "Main/FSM/Leader States/Coordinate Reinforcements")]
    public class LeaderCoordinateReinforcementsState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Leader leader)
            {
                Vector3 center = leader.PendingRequestPos;
                Vector3 lastPlayer = leader.PendingPlayerPos;

                leader.AssignReinforcements(center, lastPlayer);
                leader.ConsumeRequest();

                leader.LeaderStateTimer = 0f;
                MyLogger.LogInfo($"[Leader] CoordinateReinforcements -> center {center}, lastPlayer {lastPlayer}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Leader leader)
            {
                leader.LeaderStateTimer += Time.deltaTime;
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
        }
    }
}
