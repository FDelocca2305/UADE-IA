using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardCoverState", menuName = "Main/FSM/Guard States/Cover State")]
    public class GuardCoverState : State
    {
        [SerializeField] private float coverArrivalTolerance = 0.75f;
        [SerializeField] private float suppressFireCooldown = 0.35f;
        [SerializeField] private float occlusionCheckStep = 0.5f;
        [SerializeField] private int occlusionMaxSteps = 4;
        [SerializeField] private float threatMoveThreshold = 1.75f;

        private readonly RaycastHit[] m_coverHits = new RaycastHit[6];
        private float m_nextSuppressFireTime;
        private Vector3 m_lastThreatPosition = Vector3.zero;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StateTimer = 0f;
                guard.ClearCoverPoint();
                m_nextSuppressFireTime = 0f;

                var referencePosition = GetThreatPosition(guard);
                if (referencePosition != Vector3.zero)
                    guard.LastKnownPlayerPosition = referencePosition;

                AcquireCover(guard, true, referencePosition, guard.GetTargetTransform());
                MyLogger.LogDebug($"Guard {guard.name}: Entered Cover State - seeking cover");
                MyLogger.LogInfo($"[CoverDebug] Guard {guard.name} entered Cover State");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Guard guard) return;

            guard.StateTimer += Time.deltaTime;

            var referencePosition = GetThreatPosition(guard);
            if (referencePosition != Vector3.zero)
                guard.LastKnownPlayerPosition = referencePosition;

            if (ShouldReacquireCover(guard, referencePosition))
            {
                AcquireCover(guard, false, referencePosition, guard.GetTargetTransform());
            }

            MoveToCover(guard);
            AimAndSuppress(guard);
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.ClearCoverPoint();
                MyLogger.LogDebug($"Guard {guard.name}: Exited Cover State");
            }
        }

        private Vector3 GetThreatPosition(Guard guard)
        {
            Transform target = guard.GetTargetTransform();
            return target != null ? target.position : guard.LastKnownPlayerPosition;
        }

        private bool ShouldReacquireCover(Guard guard, Vector3 referencePosition)
        {
            if (!guard.HasCoverPoint) return true;

            bool timeElapsed = guard.StateTimer >= guard.CoverRepositionCooldown;
            bool threatMoved = (referencePosition - m_lastThreatPosition).sqrMagnitude >= threatMoveThreshold * threatMoveThreshold;
            bool exposed = !IsOccludedByCover(guard, referencePosition, guard.CoverPoint);

            return timeElapsed && (threatMoved || exposed);
        }

        private void AcquireCover(Guard guard, bool force, Vector3 referencePosition, Transform target)
        {
            if (referencePosition == Vector3.zero && !force && guard.HasCoverPoint) return;

            Vector3 awayFromThreat = guard.transform.position - referencePosition;
            if (awayFromThreat.sqrMagnitude < 0.01f)
            {
                awayFromThreat = -guard.transform.forward;
            }

            Vector3 awayDir = awayFromThreat.normalized;
            Vector3 origin = guard.transform.position + Vector3.up * 0.5f;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                guard.CoverProbeRadius,
                awayDir,
                m_coverHits,
                guard.CoverProbeDistance,
                guard.ObstaclesMask);

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

                // Robust hit data (some meshes report zeroed hit.point/normal)
                Vector3 impactPoint = bestHit.point;
                Vector3 normal = bestHit.normal;
                if (impactPoint == Vector3.zero)
                {
                    impactPoint = bestHit.collider.bounds.ClosestPoint(guard.transform.position);
                }
                if (normal == Vector3.zero)
                {
                    normal = (impactPoint - bestHit.collider.bounds.center).normalized;
                }

                // Usar la dirección opuesta al jugador para quedar detrás del obstáculo
                Vector3 threatDir = (referencePosition - impactPoint);
                threatDir.y = 0f;
                if (threatDir.sqrMagnitude < 0.01f)
                {
                    threatDir = guard.transform.forward;
                }
                threatDir.Normalize();

                Vector3 coverPoint = impactPoint - threatDir * guard.CoverOffsetFromObstacle;

                coverPoint = EnsureOccludedFromThreat(guard, referencePosition, coverPoint, normal, threatDir);
                coverPoint.y = guard.transform.position.y;
                guard.SetCoverPoint(coverPoint);
                guard.SetCoverDebug(bestHit.collider, impactPoint, normal);
                MyLogger.LogInfo($"[CoverDebug] Guard {guard.name} found cover on {bestHit.collider.name} at {impactPoint}, normal {normal}, coverPoint {coverPoint}, guardPos {guard.transform.position}, colliderPos {bestHit.collider.transform.position}");
            }
            else
            {
                Vector3 fallbackCover = guard.transform.position + awayDir * Mathf.Max(guard.CoverOffsetFromObstacle, 1f);
                guard.SetCoverPoint(fallbackCover);
                guard.SetCoverDebug(null, Vector3.zero, Vector3.zero);
                MyLogger.LogInfo($"[CoverDebug] Guard {guard.name} no obstacle hit. Fallback coverPoint {fallbackCover}");
            }

            guard.StateTimer = 0f;
            m_lastThreatPosition = referencePosition;
        }

        private Vector3 EnsureOccludedFromThreat(Guard guard, Vector3 threatPos, Vector3 coverPoint, Vector3 coverNormal, Vector3 threatDir)
        {
            if (threatPos == Vector3.zero) return coverPoint;

            Vector3 origin = threatPos + Vector3.up * 0.5f;
            Vector3 target = coverPoint + Vector3.up * 0.5f;

            // Si ya está bloqueado por un obstáculo en la máscara, mantenemos
            if (Physics.Raycast(origin, (target - origin).normalized, out var hit, Mathf.Infinity, guard.ObstaclesMask))
            {
                if (hit.collider == guard.LastCoverCollider)
                    return coverPoint;
            }

            // Empujar el punto de cover hacia adentro del obstáculo/normal para romper línea de visión
            Vector3 adjusted = coverPoint;
            for (int i = 0; i < occlusionMaxSteps; i++)
            {
                // Empuja en la misma dirección contraria al jugador para garantizar ocultamiento
                adjusted -= threatDir * occlusionCheckStep;
                Vector3 adjTarget = adjusted + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, (adjTarget - origin).normalized, out var adjHit, Mathf.Infinity, guard.ObstaclesMask))
                {
                    if (adjHit.collider == guard.LastCoverCollider)
                    {
                        return adjusted;
                    }
                }
            }

            return adjusted;
        }

        private bool IsOccludedByCover(Guard guard, Vector3 threatPos, Vector3 coverPoint)
        {
            if (threatPos == Vector3.zero) return true;
            Vector3 origin = threatPos + Vector3.up * 0.5f;
            Vector3 target = coverPoint + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, (target - origin).normalized, out var hit, Mathf.Infinity, guard.ObstaclesMask))
            {
                return hit.collider == guard.LastCoverCollider;
            }
            return false;
        }

        private void MoveToCover(Guard guard)
        {
            if (!guard.HasCoverPoint) return;

            float distance = Vector3.Distance(guard.transform.position, guard.CoverPoint);
            if (distance > coverArrivalTolerance)
            {
                Vector3 steering = Steering.Arrive(
                    guard.transform.position,
                    guard.CoverPoint,
                    guard.CurrentVelocity,
                    guard.PatrolSpeed,
                    guard.SlowingDistance);

                guard.ApplySteering(steering);
            }
            else
            {
                guard.ApplySteering(Vector3.zero);
            }
        }

        private void AimAndSuppress(Guard guard)
        {
            Vector3 lookTarget = guard.LastKnownPlayerPosition;
            Transform target = guard.GetTargetTransform();

            if (target != null && guard.CanSeePlayer())
            {
                lookTarget = target.position;
            }

            // Siempre mirar hacia la última posición conocida del jugador mientras está en cover
            if (lookTarget == Vector3.zero && guard.LastKnownPlayerPosition != Vector3.zero)
            {
                lookTarget = guard.LastKnownPlayerPosition;
            }

            Vector3 aimDir = lookTarget - guard.transform.position;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.1f) return;

            guard.FaceDirection(aimDir.normalized, guard.BaseRotationSpeed * 2f);

            if (Time.time < m_nextSuppressFireTime) return;

            bool playerLikelyThere = guard.CanSeePlayer() || guard.RecentlySawPlayer(guard.LosePlayerTimeout);
            float distanceToLastKnown = Vector3.Distance(guard.transform.position, guard.LastKnownPlayerPosition);
            bool withinRange = distanceToLastKnown <= guard.AttackRange + 1.5f;

            if (playerLikelyThere && withinRange)
            {
                guard.Shoot(aimDir.normalized);
                m_nextSuppressFireTime = Time.time + suppressFireCooldown;
            }
        }
    }
}
