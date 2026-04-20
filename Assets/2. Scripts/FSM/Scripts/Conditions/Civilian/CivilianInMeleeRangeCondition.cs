using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "InMeleeRangeCondition", menuName = "Main/FSM/Civilian Conditions/In Melee Range")]
public class CivilianInMeleeRangeCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            float distanceToPlayer = civilian.GetDistanceToPlayer();
            bool inMeleeRange = distanceToPlayer <= civilian.MeleeRange;
            
            return inMeleeRange;
        }
        return false;
    }
}