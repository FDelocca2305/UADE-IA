using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllySearchGuardState", menuName = "Main/FSM/Ally States/Search Guard")]
    public class AllySearchGuardState : State
    {
        [SerializeField] private float searchRadius = 2.5f;
        [SerializeField] private float waypointInterval = 0.75f;
        private float m_nextWaypointTime;
        private Vector3 m_currentSearchTarget;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                m_nextWaypointTime = 0f;
                PickNewSearchTarget(ally);
                MyLogger.LogDebug($"Ally {ally.name}: Enter Search around {ally.LastKnownGuardPosition}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Ally ally) return;

            ally.StateTimer += Time.deltaTime;

            if (ally.StateTimer >= ally.SearchDuration)
                return;

            if (Time.time >= m_nextWaypointTime || m_currentSearchTarget == Vector3.zero)
            {
                PickNewSearchTarget(ally);
            }

            ally.MoveTowardsPoint(
                m_currentSearchTarget,
                ally.FollowSpeed,
                ally.InvestigationArrivalTolerance);
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StopMovement();
                MyLogger.LogDebug($"Ally {ally.name}: Exit Search");
            }
        }

        private void PickNewSearchTarget(Ally ally)
        {
            Vector3 origin = ally.LastKnownGuardPosition != Vector3.zero
                ? ally.LastKnownGuardPosition
                : ally.transform.position;

            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            m_currentSearchTarget = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            m_nextWaypointTime = Time.time + waypointInterval;
        }
    }
}
