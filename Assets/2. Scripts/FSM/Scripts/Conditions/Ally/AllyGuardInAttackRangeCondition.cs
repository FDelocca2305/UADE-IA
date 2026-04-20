using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyGuardInAttackRangeCondition", menuName = "Main/FSM/Ally Conditions/Guard In Attack Range")]
public class AllyGuardInAttackRangeCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            if (ally.CoverLockActive) return false;
            return ally.IsGuardInAttackRange();
        }

        return false;
    }
}
