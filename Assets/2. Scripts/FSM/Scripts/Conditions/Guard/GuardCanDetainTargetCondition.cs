using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "GuardCanDetainTargetCondition", menuName = "Main/FSM/Guard Conditions/Can Detain Target")]
public class GuardCanDetainTargetCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            if (guard.DetainTarget == null) return false;
            if (!guard.IsActive) return false;

            // Must have line of sight and be behind the target (target not facing guard)
            Transform target = guard.DetainTarget;
            Vector3 toGuard = (guard.transform.position - target.position).normalized;
            float targetBackAngle = Vector3.Angle(target.forward, toGuard);

            // Consider "behind" when angle is close to 180 (target nos mira)
            bool targetNotFacing = targetBackAngle >= 180f - guard.DetainBackAngle * 0.5f;
            bool canSee = guard.CanSeePlayer(); // reuse detection; assumes target on same layer/logic

            return targetNotFacing && canSee;
        }

        return false;
    }
}
