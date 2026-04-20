using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderHasPendingRequestCondition", menuName = "Main/FSM/Leader Conditions/Has Pending Request")]
public class LeaderHasPendingRequestCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            return leader.HasPendingRequest;
        }
        return false;
    }
}
