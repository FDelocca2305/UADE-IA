using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "LeaderCoverFireState", menuName = "Main/FSM/Leader States/Cover Fire")]
    public class LeaderCoverFireState : State
    {
        private float m_nextBurstTime;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Leader leader)
            {
                leader.LeaderStateTimer = 0f;
                m_nextBurstTime = 0f;
                leader.PauseMovement(true);

                Vector3 targetPos = leader.LastKnownPlayerPosition;
                if (targetPos == Vector3.zero && leader.GetTargetTransform() != null)
                {
                    targetPos = leader.GetTargetTransform().position;
                }

                // Envía refuerzos hacia la última posición conocida del player
                leader.AssignReinforcements(targetPos, targetPos);

                MyLogger.LogInfo($"[Leader] CoverFire -> target {targetPos}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Leader leader) return;

            leader.LeaderStateTimer += Time.deltaTime;

            Vector3 targetPos = leader.LastKnownPlayerPosition;
            if (targetPos == Vector3.zero && leader.GetTargetTransform() != null)
            {
                targetPos = leader.GetTargetTransform().position;
            }

            FireBurst(leader, targetPos);
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Leader leader)
            {
                // Limpia órdenes si sale de cover fire y vuelve a habilitar movimiento.
                leader.PauseMovement(false);
                leader.ClearAllOverrides();
                MyLogger.LogInfo("[Leader] CoverFire exit - cleared overrides");
            }
        }

        private void FireBurst(Leader leader, Vector3 targetPos)
        {
            if (targetPos == Vector3.zero) return;
            if (Time.time < m_nextBurstTime) return;

            Vector3 dir = targetPos - leader.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f) return;

            leader.FaceDirection(dir.normalized, leader.BaseRotationSpeed * 2f);
            FireFan(leader, dir.normalized);

            m_nextBurstTime = Time.time + leader.CoverFireFireRate;
        }

        private void FireFan(Leader leader, Vector3 forward)
        {
            float[] angles = { 0f, -12f, 12f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * forward;
                leader.Shoot(dir);
            }
        }
    }
}
