using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "CanSeePlayerCondition", menuName = "Main/FSM/Guard Conditions/Can See Player")]
public class GuardCanSeePlayerCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.CanSeePlayer();
        }
        return false;
    }
}