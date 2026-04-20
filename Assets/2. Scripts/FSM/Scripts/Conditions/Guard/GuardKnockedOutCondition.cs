using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "GuardKnockedOutCondition", menuName = "Main/FSM/Guard Conditions/Knocked Out")]
public class GuardKnockedOutCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.IsKnockedOut;
        }

        return false;
    }
}
