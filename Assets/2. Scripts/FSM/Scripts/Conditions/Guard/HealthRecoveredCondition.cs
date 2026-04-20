using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthRecoveredCondition", menuName = "Main/FSM/Guard Conditions/Health Recovered")]
public class GuardHealthRecoveredCondition : StateCondition
{
    [SerializeField, Range(0f, 1f)] private float recoveredThreshold = 0.8f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.HealthNormalized >= recoveredThreshold;
        }

        return false;
    }
}
