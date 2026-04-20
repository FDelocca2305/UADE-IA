using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "LeaderHoldPerimeterState", menuName = "Main/FSM/Leader States/Hold Perimeter")]
    public class LeaderHoldPerimeterState : State
    {
        [SerializeField] private float holdFromRequestDuration = 6f;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Leader leader)
            {
                Vector3 center = leader.PendingRequestPos != Vector3.zero
                    ? leader.PendingRequestPos
                    : leader.transform.position;

                leader.BeginHold(center);
                leader.LeaderStateTimer = 0f;

                MyLogger.LogInfo($"[Leader] HoldPerimeter -> center {center}");
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
            if (p_model is Leader leader)
            {
                leader.ClearAllOverrides();
                MyLogger.LogInfo("[Leader] HoldPerimeter exit - cleared overrides");
            }
        }
    }
}
