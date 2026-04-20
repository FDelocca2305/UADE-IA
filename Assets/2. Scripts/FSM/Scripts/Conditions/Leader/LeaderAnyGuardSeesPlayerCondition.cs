using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderAnyGuardSeesPlayerCondition", menuName = "Main/FSM/Leader Conditions/Any Guard Sees Player")]
public class LeaderAnyGuardSeesPlayerCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            var guards = leader.ManagedGuards;
            if (guards == null) return false;

            foreach (var guard in guards)
            {
                if (guard != null && guard.IsActive && guard.CanSeePlayer())
                    return true;
            }
        }

        return false;
    }
}
