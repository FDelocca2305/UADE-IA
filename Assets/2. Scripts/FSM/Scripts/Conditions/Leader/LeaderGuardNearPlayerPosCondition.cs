using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderGuardNearPlayerPosCondition", menuName = "Main/FSM/Leader Conditions/Guard Near Player Pos")]
public class LeaderGuardNearPlayerPosCondition : StateCondition
{
    [SerializeField] private float distanceThreshold = 3f;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Leader leader)
        {
            var guards = leader.ManagedGuards;
            if (guards == null) return false;

            Vector3 targetPos = leader.LastKnownPlayerPosition;
            if (targetPos == Vector3.zero && leader.GetTargetTransform() != null)
            {
                targetPos = leader.GetTargetTransform().position;
            }
            if (targetPos == Vector3.zero) return false;

            float thresholdSqr = distanceThreshold * distanceThreshold;
            foreach (var guard in guards)
            {
                if (guard == null || !guard.IsActive) continue;
                float distSqr = (guard.transform.position - targetPos).sqrMagnitude;
                if (distSqr <= thresholdSqr)
                    return true;
            }
        }

        return false;
    }
}
