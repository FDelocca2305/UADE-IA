using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyTakeCoverState", menuName = "Main/FSM/Ally States/Take Cover")]
    public class AllyTakeCoverState : State
    {
        [SerializeField] private float suppressFireCooldown = 0.45f;
        [SerializeField] private float threatMoveThreshold = 1.75f;
        [SerializeField] private float coverProbeHeightOffset = 0.5f;
        [SerializeField] private float fallbackCoverDistance = 2f;
        [SerializeField] private float occlusionCheckStep = 0.5f;
        [SerializeField] private int occlusionMaxSteps = 4;

        private readonly RaycastHit[] m_coverHits = new RaycastHit[6];
        private float m_nextSuppressFireTime;
        private Vector3 m_lastThreatPosition;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                m_nextSuppressFireTime = 0f;
                m_lastThreatPosition = Vector3.zero;
                ally.LastTimeTookCover = Time.time;
                ally.ClearCoverPoint();
                ally.StartCoverLock(ally.CoverMinDuration);

                Vector3 threat = GetThreatPosition(ally);
                if (threat != Vector3.zero)
                {
                    ally.LastKnownGuardPosition = threat;
                }

                AcquireCover(ally, true, threat);
                MyLogger.LogDebug($"Ally {ally.name}: Enter TakeCover");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Ally ally) return;

            ally.StateTimer += Time.deltaTime;

            Vector3 threat = GetThreatPosition(ally);
            if (threat != Vector3.zero)
            {
                ally.LastKnownGuardPosition = threat;
            }

            if (ShouldReacquireCover(ally, threat))
            {
                AcquireCover(ally, false, threat);
            }

            MoveToCover(ally);
            RegenerateIfSafe(ally);
            AimAndSuppress(ally, threat);
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.ClearCoverPoint();
                MyLogger.LogDebug($"Ally {ally.name}: Exit TakeCover");
            }
        }

        private Vector3 GetThreatPosition(Ally ally)
        {
            Transform target = ally.GetTargetTransform();
            if (target != null && ally.CanSeeGuard(ally.GetCurrentTarget()))
            {
                return target.position;
            }

            return ally.LastKnownGuardPosition;
        }

        private bool ShouldReacquireCover(Ally ally, Vector3 reference)
        {
            if (!ally.HasCoverPoint) return true;

            bool timeElapsed = ally.StateTimer >= ally.CoverRepositionCooldown;
            bool threatMoved = (reference - m_lastThreatPosition).sqrMagnitude >= threatMoveThreshold * threatMoveThreshold;
            bool exposed = !IsOccludedByCover(ally, reference, ally.CoverPoint);

            return timeElapsed && (threatMoved || exposed);
        }

        private void AcquireCover(Ally ally, bool force, Vector3 referencePosition)
        {
            if (referencePosition == Vector3.zero && !force && ally.HasCoverPoint) return;

            Vector3 awayDir = (ally.transform.position - referencePosition).normalized;
            if (awayDir.sqrMagnitude < 0.01f)
            {
                awayDir = -ally.transform.forward;
            }

            Vector3 origin = ally.transform.position + Vector3.up * coverProbeHeightOffset;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                ally.CoverProbeRadius,
                awayDir,
                m_coverHits,
                ally.CoverProbeDistance,
                ally.ObstaclesMask);

            if (hitCount > 0)
            {
                RaycastHit bestHit = m_coverHits[0];
                for (int i = 1; i < hitCount; i++)
                {
                    if (m_coverHits[i].distance < bestHit.distance)
                    {
                        bestHit = m_coverHits[i];
                    }
                }

                Vector3 impactPoint = bestHit.point;
                Vector3 normal = bestHit.normal;
                if (impactPoint == Vector3.zero)
                {
                    impactPoint = bestHit.collider.bounds.ClosestPoint(ally.transform.position);
                }
                if (normal == Vector3.zero)
                {
                    normal = (impactPoint - bestHit.collider.bounds.center).normalized;
                }

                Vector3 threatDir = referencePosition != Vector3.zero
                    ? (referencePosition - impactPoint)
                    : ally.transform.forward;
                threatDir.y = 0f;
                if (threatDir.sqrMagnitude < 0.01f) threatDir = ally.transform.forward;
                threatDir.Normalize();

                Vector3 coverPoint = impactPoint - threatDir * ally.CoverOffsetFromObstacle;
                coverPoint = EnsureOccludedFromThreat(ally, referencePosition, coverPoint, normal, threatDir, bestHit.collider);
                coverPoint.y = ally.transform.position.y;

                ally.SetCoverPoint(coverPoint);
                ally.SetCoverDebug(bestHit.collider, impactPoint, normal);
            }
            else
            {
                Vector3 fallback = ally.transform.position + awayDir * Mathf.Max(fallbackCoverDistance, ally.CoverOffsetFromObstacle);
                fallback.y = ally.transform.position.y;
                ally.SetCoverPoint(fallback);
                ally.SetCoverDebug(null, Vector3.zero, Vector3.zero);
            }

            ally.StateTimer = 0f;
            m_lastThreatPosition = referencePosition;
        }

        private void MoveToCover(Ally ally)
        {
            if (!ally.HasCoverPoint) return;

            ally.MoveTowardsPoint(
                ally.CoverPoint,
                ally.FollowSpeed,
                ally.CoverArrivalTolerance);
        }

        private void RegenerateIfSafe(Ally ally)
        {
            if (!ally.HasCoverPoint) return;

            float distance = Vector3.Distance(ally.transform.position, ally.CoverPoint);
            if (distance > ally.CoverArrivalTolerance * 1.05f) return;

            ally.RegenerateHealth(ally.CoverHealthRegenPerSecond * Time.deltaTime);
        }

        private void AimAndSuppress(Ally ally, Vector3 threat)
        {
            Vector3 lookTarget = threat != Vector3.zero ? threat : ally.LastKnownGuardPosition;
            Vector3 aimDir = lookTarget - ally.transform.position;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.1f)
            {
                ally.FaceDirectionTowards(aimDir.normalized);
            }

            if (Time.time < m_nextSuppressFireTime) return;

            float distance = lookTarget != Vector3.zero
                ? Vector3.Distance(ally.transform.position, lookTarget)
                : Mathf.Infinity;

            bool threatLikelyThere = (ally.GetCurrentTarget() != null && ally.CanSeeGuard(ally.GetCurrentTarget()))
                                     || (threat != Vector3.zero && Time.time - ally.LastTimeSawGuard <= ally.LoseGuardDelay);

            if (threatLikelyThere && distance <= ally.AttackRange + 1.5f && ally.CanShoot())
            {
                ally.Shoot(aimDir.normalized);
                m_nextSuppressFireTime = Time.time + suppressFireCooldown;
            }
        }

        private bool IsOccludedByCover(Ally ally, Vector3 threatPos, Vector3 coverPoint)
        {
            if (threatPos == Vector3.zero) return true;
            Vector3 origin = threatPos + Vector3.up * 0.5f;
            Vector3 target = coverPoint + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, (target - origin).normalized, out var hit, Mathf.Infinity, ally.ObstaclesMask))
            {
                return hit.collider != null && hit.collider == ally.LastCoverCollider;
            }
            return false;
        }

        private Vector3 EnsureOccludedFromThreat(Ally ally, Vector3 threatPos, Vector3 coverPoint, Vector3 coverNormal, Vector3 threatDir, Collider coverCollider)
        {
            if (threatPos == Vector3.zero) return coverPoint;

            Vector3 origin = threatPos + Vector3.up * 0.5f;
            Vector3 target = coverPoint + Vector3.up * 0.5f;

            // If already blocked by obstacle, keep
            if (Physics.Raycast(origin, (target - origin).normalized, out var hit, Mathf.Infinity, ally.ObstaclesMask))
            {
                if (hit.collider == coverCollider)
                    return coverPoint;
            }

            Vector3 adjusted = coverPoint;
            for (int i = 0; i < occlusionMaxSteps; i++)
            {
                adjusted -= threatDir * occlusionCheckStep;
                Vector3 adjTarget = adjusted + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, (adjTarget - origin).normalized, out var adjHit, Mathf.Infinity, ally.ObstaclesMask))
                {
                    if (adjHit.collider == coverCollider)
                    {
                        return adjusted;
                    }
                }
            }

            return adjusted;
        }
    }
}
