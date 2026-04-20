using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyStateTimerCondition", menuName = "Main/FSM/Ally Conditions/State Timer")]
public class AllyStateTimerCondition : StateCondition
{
    [SerializeField] private float duration = 0.5f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            return ally.StateTimer >= duration;
        }

        return false;
    }
}
