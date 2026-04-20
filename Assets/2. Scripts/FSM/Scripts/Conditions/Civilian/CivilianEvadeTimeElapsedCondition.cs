using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "EvadeTimeElapsedCondition", menuName = "Main/FSM/Civilian Conditions/Evade Time Elapsed")]
public class CivilianEvadeTimeElapsedCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            // Check if the evade time has elapsed
            return civilian.StateTimer >= civilian.EvadeTime;
        }
        return false;
    }
}