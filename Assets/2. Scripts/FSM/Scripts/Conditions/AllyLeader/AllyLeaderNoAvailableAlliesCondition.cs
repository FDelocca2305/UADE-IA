using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderNoAvailableAlliesCondition", menuName = "Main/FSM/AllyLeader Conditions/No Available Allies")]
public class AllyLeaderNoAvailableAlliesCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            return !leader.HasAvailableAllies();
        }

        return false;
    }
}
