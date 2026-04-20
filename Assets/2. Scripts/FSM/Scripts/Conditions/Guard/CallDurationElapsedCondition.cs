using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "CallDurationElapsedCondition", menuName = "Main/FSM/Guard Conditions/Call Duration Elapsed")]
public class GuardCallDurationElapsedCondition : StateCondition
{
    [SerializeField] private float duration = 1.5f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.StateTimer >= duration;
        }

        return false;
    }
}
