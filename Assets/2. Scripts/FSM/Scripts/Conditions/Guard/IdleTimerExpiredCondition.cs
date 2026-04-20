using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "IdleTimerExpiredCondition", menuName = "Main/FSM/Guard Conditions/Idle Timer Expired")]
public class IdleTimerExpiredCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.StateTimer <= 0f;
        }
        return false;
    }
}