using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackCompleteCondition", menuName = "Main/FSM/Civilian Conditions/Attack Complete")]
public class CivilianAttackCompleteCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            // Check if the full attack cycle has completed
            float totalAttackTime = civilian.AttackWindup + civilian.AttackHitWin + civilian.AttackRecover;
            return civilian.StateTimer >= totalAttackTime;
        }
        return false;
    }
}