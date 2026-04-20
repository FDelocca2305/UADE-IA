using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "SearchTimeElapsedCondition", menuName = "Main/FSM/Guard Conditions/Search Time Elapsed")]
public class SearchTimeElapsedCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.StateTimer >= guard.SearchTime;
        }
        return false;
    }
}