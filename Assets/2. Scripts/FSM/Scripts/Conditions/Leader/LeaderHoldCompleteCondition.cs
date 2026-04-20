using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderHoldCompleteCondition", menuName = "Main/FSM/Leader Conditions/Hold Complete")]
public class LeaderHoldCompleteCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            return leader.HoldComplete;
        }
        return false;
    }
}
