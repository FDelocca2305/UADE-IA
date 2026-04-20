using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyInvestigationCompleteCondition", menuName = "Main/FSM/Ally Conditions/Investigation Complete")]
public class AllyInvestigationCompleteCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            return ally.InvestigationComplete;
        }

        return false;
    }
}
