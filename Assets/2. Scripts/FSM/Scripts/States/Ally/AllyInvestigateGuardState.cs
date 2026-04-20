using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyInvestigateGuardState", menuName = "Main/FSM/Ally States/Investigate Guard")]
    public class AllyInvestigateGuardState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                Vector3 targetPos = ally.GetCurrentTarget() != null
                    ? ally.GetCurrentTarget().transform.position
                    : ally.LastKnownGuardPosition;

                ally.BeginInvestigation(targetPos);
                ally.StateTimer = 0f;
                MyLogger.LogDebug($"Ally {ally.name}: Enter Investigate at {targetPos}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Ally ally) return;

            ally.StateTimer += Time.deltaTime;

            if (!ally.InvestigationAtLocation)
            {
                Vector3 targetPos = ally.InvestigationTarget != Vector3.zero
                    ? ally.InvestigationTarget
                    : ally.LastKnownGuardPosition;

                ally.MoveTowardsPoint(
                    targetPos,
                    ally.FollowSpeed * ally.InvestigationMoveSpeedFactor,
                    ally.InvestigationArrivalTolerance);

                float distance = Vector3.Distance(ally.transform.position, targetPos);
                if (distance <= ally.InvestigationArrivalTolerance)
                {
                    ally.MarkInvestigationArrived();
                    ally.StopMovement();
                }
            }
            else
            {
                ally.StepInvestigationScan();
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StopMovement();
                MyLogger.LogDebug($"Ally {ally.name}: Exit Investigate");
            }
        }
    }
}
