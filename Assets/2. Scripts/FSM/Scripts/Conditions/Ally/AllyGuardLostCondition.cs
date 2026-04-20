using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyGuardLostCondition", menuName = "Main/FSM/Ally Conditions/Guard Lost")]
public class AllyGuardLostCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Ally ally)
        {
            if (ally.CoverLockActive) return false;

            var target = ally.GetCurrentTarget();
            if (target == null) return true;
            if (!target.IsAlive) return true;

            bool lostLineOfSight = !ally.CanSeeGuard(target);
            bool timeExpired = Time.time - ally.LastTimeSawGuard > ally.LoseGuardDelay;

            // Si perdimos LoS por un tiempo, gatillamos Investigate/Search aunque estemos cerca.
            if (lostLineOfSight && timeExpired) return true;

            // Si está fuera de rango de ataque y pasó el delay, también lo consideramos perdido.
            if (!ally.IsGuardInAttackRange() && timeExpired) return true;

            return false;
        }

        return false;
    }
}
