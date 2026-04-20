using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyCoverSecuredCondition", menuName = "Main/FSM/Ally Conditions/Cover Secured")]
public class AllyCoverSecuredCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            if (ally.CoverLockActive) return false;

            if (!ally.HasCoverPoint) return false;

            float distance = Vector3.Distance(ally.transform.position, ally.CoverPoint);
            if (distance > ally.CoverArrivalTolerance) return false;

            float maxHealth = Mathf.Max(ally.MaxHealth, 0.01f);
            float healthPercent = ally.CurrentHealth / maxHealth;
            return healthPercent >= ally.CoverExitHealthPercent;
        }

        return false;
    }
}
