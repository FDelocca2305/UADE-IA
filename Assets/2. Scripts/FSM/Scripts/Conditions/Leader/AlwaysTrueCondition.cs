using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderAlwaysTrueCondition", menuName = "Main/FSM/Leader Conditions/Always True")]
public class LeaderAlwaysTrueCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        // Returns true every tick; useful for immediate transitions.
        return true;
    }
}
