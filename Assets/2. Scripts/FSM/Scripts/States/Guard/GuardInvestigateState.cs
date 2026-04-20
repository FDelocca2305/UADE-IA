using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardInvestigateState", menuName = "Main/FSM/Guard States/Investigate State")]
    public class GuardInvestigateState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                Vector3 targetPos = guard.LastKnownPlayerPosition;
                if (targetPos == Vector3.zero && guard.GetTargetTransform() != null)
                {
                    targetPos = guard.GetTargetTransform().position;
                }

                guard.BeginInvestigation(targetPos);
                guard.StateTimer = 0f;
                MyLogger.LogInfo($"[InvestigateDebug] Guard {guard.name} entered Investigate at {targetPos}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Guard guard) return;

            guard.StateTimer += Time.deltaTime;

            if (!guard.InvestigationAtLocation)
            {
                MoveTowardsInvestigationPoint(guard);
            }
            else
            {
                RotateScan(guard);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                MyLogger.LogInfo($"[InvestigateDebug] Guard {guard.name} exited Investigate");
            }
        }

        private void MoveTowardsInvestigationPoint(Guard guard)
        {
            Vector3 targetPos = guard.InvestigationTarget;
            if (targetPos == Vector3.zero) targetPos = guard.transform.position;

            float distance = Vector3.Distance(guard.transform.position, targetPos);
            if (distance > guard.InvestigationArrivalTolerance)
            {
                Vector3 steering = Steering.Arrive(
                    guard.transform.position,
                    targetPos,
                    guard.CurrentVelocity,
                    guard.PatrolSpeed * guard.InvestigationMoveSpeedFactor,
                    guard.SlowingDistance);

                guard.ApplySteering(steering);
            }
            else
            {
                guard.ApplySteering(Vector3.zero);
                guard.MarkInvestigationArrived();
                MyLogger.LogInfo($"[InvestigateDebug] Guard {guard.name} arrived investigation point");
            }
        }

        private void RotateScan(Guard guard)
        {
            if (guard.InvestigationComplete) return;

            float rotateAmount = guard.InvestigationRotateSpeed * Time.deltaTime;
            guard.InvestigationRotationRemaining -= rotateAmount;

            guard.transform.Rotate(0f, rotateAmount, 0f);

            if (guard.InvestigationRotationRemaining <= 0f)
            {
                guard.CompleteInvestigation();
                MyLogger.LogInfo($"[InvestigateDebug] Guard {guard.name} completed 360 scan");
            }
        }
    }
}
