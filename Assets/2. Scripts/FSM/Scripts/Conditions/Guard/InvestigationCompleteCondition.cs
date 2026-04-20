using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "InvestigationCompleteCondition", menuName = "Main/FSM/Guard Conditions/Investigation Complete")]
public class GuardInvestigationCompleteCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.InvestigationComplete;
        }

        return false;
    }
}
