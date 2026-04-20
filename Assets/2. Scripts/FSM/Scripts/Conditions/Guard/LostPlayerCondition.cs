using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LostPlayerCondition", menuName = "Main/FSM/Guard Conditions/Player Lost")]
public class GuardLostPlayerCondition : StateCondition
{
    [SerializeField] private float loseSightTime = 3f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            if (guard.CanSeePlayer())
            {
                return false;
            }

            return guard.TimeSinceLastSeenPlayer >= loseSightTime;
        }

        return false;
    }
}
