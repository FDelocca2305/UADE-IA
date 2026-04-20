using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyGuardDetectedCondition", menuName = "Main/FSM/Ally Conditions/Guard Detected")]
public class AllyGuardDetectedCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            var target = ally.AcquireGuardTarget();
            if (target != null)
            {
                ally.SetTargetTransform(target.transform);
                return true;
            }
        }

        return false;
    }
}
