using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "IsSafeCondition", menuName = "Main/FSM/Civilian Conditions/Is Safe")]
public class CivilianIsSafeCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            // Safety requires all three conditions:
            // 1. Distance is safe (>= safeDistance)
            // 2. No line of sight to player (!HasLoS())
            // 3. Safety has been maintained for the required time (SafeTimer >= safeTime)
            
            float distanceToPlayer = civilian.GetDistanceToPlayer();
            bool isSafeDistance = distanceToPlayer >= civilian.SafeDistance;
            bool hasNoLineOfSight = !civilian.HasLoS();
            bool safeTimeElapsed = civilian.SafeTimer >= civilian.SafeTime;
            
            return isSafeDistance && hasNoLineOfSight && safeTimeElapsed;
        }
        return false;
    }
}