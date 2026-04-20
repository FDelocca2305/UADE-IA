using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderCoverFireDurationCondition", menuName = "Main/FSM/Leader Conditions/Cover Fire Duration Elapsed")]
public class LeaderCoverFireDurationCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            return leader.LeaderStateTimer >= leader.CoverFireDuration;
        }
        return false;
    }
}
