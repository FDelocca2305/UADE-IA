using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderPlayerLowHealthCondition", menuName = "Main/FSM/AllyLeader Conditions/Player Low Health")]
public class AllyLeaderPlayerLowHealthCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            if (!leader.HasPlayerHealthData()) return false;
            if (!leader.HasAvailableAllies()) return false;

            float healthPercent = leader.GetPlayerHealthPercent();
            return healthPercent <= leader.PlayerProtectThreshold;
        }

        return false;
    }
}
