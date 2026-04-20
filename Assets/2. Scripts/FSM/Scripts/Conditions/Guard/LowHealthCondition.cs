using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LowHealthCondition", menuName = "Main/FSM/Guard Conditions/Low Health")]
public class GuardLowHealthCondition : StateCondition
{
    [SerializeField, Range(0f, 1f)] private float healthThreshold = 0.35f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.HealthNormalized <= healthThreshold;
        }

        return false;
    }
}
