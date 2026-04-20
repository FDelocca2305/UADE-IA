using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyGuardOutOfAttackRangeCondition", menuName = "Main/FSM/Ally Conditions/Guard Out Of Attack Range")]
public class AllyGuardOutOfAttackRangeCondition : StateCondition
{
[SerializeField] [Range(0f, 1f)] private float extraTolerance = 0.3f;

    [SerializeField] [Range(0f, 1f)] private float lowHealthOverride = 0.35f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            if (ally.CoverLockActive) return false;
            // Si está bajo de vida preferimos evaluar el estado de cover antes que seguir persiguiendo.
            if (ally.IsLowHealth(lowHealthOverride))
                return false;

            return ally.IsGuardOutOfAttackRange(extraTolerance);
        }

        return false;
    }
}
