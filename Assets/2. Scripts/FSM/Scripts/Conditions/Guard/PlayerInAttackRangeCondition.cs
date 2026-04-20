using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInAttackRangeCondition", menuName = "Main/FSM/Guard Conditions/Player In Attack Range")]
public class PlayerInAttackRangeCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Guard guard)
        {
            Transform player = guard.GetTargetTransform();
            if (player == null) return false;

            float distance = Vector3.Distance(guard.transform.position, player.position);
            return distance <= guard.AttackRange;
        }
        return false;
    }
}