using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderTimerElapsedCondition", menuName = "Main/FSM/AllyLeader Conditions/Timer Elapsed")]
public class AllyLeaderTimerElapsedCondition : StateCondition
{
    [SerializeField] private float minSeconds = 4f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            return leader.LeaderStateTimer >= minSeconds;
        }

        return false;
    }
}
