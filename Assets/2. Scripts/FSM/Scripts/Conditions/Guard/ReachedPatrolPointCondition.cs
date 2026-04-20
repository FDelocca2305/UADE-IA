using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "ReachedPatrolPointCondition", menuName = "Main/FSM/Guard Conditions/Reached Patrol Point")]
public class ReachedPatrolPointCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            if (guard.PatrolPoints == null || guard.CurrentPatrolIndex >= guard.PatrolPoints.Length)
                return true;

            float distance = Vector3.Distance(guard.transform.position, guard.PatrolPoints[guard.CurrentPatrolIndex].position);

            if (distance < 1f)
            {
                // Move to next patrol point
                guard.CurrentPatrolIndex = (guard.CurrentPatrolIndex + 1) % guard.PatrolPoints.Length;
                return true;
            }
        }
        return false;
    }
}