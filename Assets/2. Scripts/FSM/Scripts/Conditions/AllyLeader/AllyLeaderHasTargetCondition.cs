using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderHasTargetCondition", menuName = "Main/FSM/AllyLeader Conditions/Has Target")]
public class AllyLeaderHasTargetCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            Guard l_target = leader.GetCurrentTarget();
            return l_target != null && l_target.IsAlive;
        }

        return false;
    }
}
