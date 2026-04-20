using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "PatrolLoopsCompletedCondition", menuName = "Main/FSM/Guard Conditions/Patrol Loops Completed")]
public class PatrolLoopsCompletedCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.CurrentPatrolLoops >= guard.LoopsToIdle;
        }
        return false;
    }
}