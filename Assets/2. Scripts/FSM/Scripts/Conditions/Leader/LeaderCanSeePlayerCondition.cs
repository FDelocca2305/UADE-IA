using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderCanSeePlayerCondition", menuName = "Main/FSM/Leader Conditions/Can See Player")]
public class LeaderCanSeePlayerCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            return leader.CanSeePlayer();
        }
        return false;
    }
}
