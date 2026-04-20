using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderPlayerRecoveredCondition", menuName = "Main/FSM/AllyLeader Conditions/Player Recovered")]
public class AllyLeaderPlayerRecoveredCondition : StateCondition
{
    [SerializeField] private float recoveryMargin = 0.2f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            if (!leader.HasPlayerHealthData()) return false;

            float healthPercent = leader.GetPlayerHealthPercent();
            return leader.IsProtectingPlayer && healthPercent >= leader.PlayerProtectThreshold + recoveryMargin;
        }

        return false;
    }
}
