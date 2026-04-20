using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "RecentDamageCondition", menuName = "Main/FSM/Guard Conditions/Recent Damage")]
public class GuardRecentDamageCondition : StateCondition
{
    [SerializeField] private float recentWindow = 3f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            return guard.TookDamageRecently(recentWindow);
        }

        return false;
    }
}
