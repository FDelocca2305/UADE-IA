using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AlwaysFalseCondition", menuName = "Main/FSM/Civilian Conditions/Always False")]
public class AlwaysFalseCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        // Always return false - only Decision Tree can trigger transitions
        return false;
    }
}