using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllySearchExpiredCondition", menuName = "Main/FSM/Ally Conditions/Search Expired")]
public class AllySearchExpiredCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            return ally.StateTimer >= ally.SearchDuration;
        }

        return false;
    }
}
