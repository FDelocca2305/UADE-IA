using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyNeedsCoverCondition", menuName = "Main/FSM/Ally Conditions/Needs Cover")]
public class AllyNeedsCoverCondition : StateCondition
{
    [SerializeField, Range(0f, 1f)] private float healthThreshold = 0.35f;
    [SerializeField] private float minTimeBetweenCovers = 1.0f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            if (ally.CoverLockActive) return false;
            float maxHealth = Mathf.Max(ally.MaxHealth, 0.01f);
            float healthPercent = ally.CurrentHealth / maxHealth;

            if (healthPercent > healthThreshold) return false;
            if (ally.GetCurrentTarget() == null) return false;
            if (Time.time - ally.LastTimeTookCover < Mathf.Max(minTimeBetweenCovers, ally.CoverReenterCooldown))
                return false;

            return true;
        }

        return false;
    }
}
