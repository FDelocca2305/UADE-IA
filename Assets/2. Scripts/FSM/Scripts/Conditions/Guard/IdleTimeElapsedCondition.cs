using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "IdleTimeElapsedCondition", menuName = "Main/FSM/Guard Conditions/Idle Time Elapsed")]
public class IdleTimeElapsedCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.StateTimer >= guard.IdleTime;
        }
        return false;
    }
}