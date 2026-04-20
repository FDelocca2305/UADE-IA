using UnityEngine;
using Game.AI.Steering;
using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using Services;
using Services.MicroServices.UpdateService;
using ScriptableObjects.Bullets;
using Services.MicroServices.PoolObjectsService;
using System.Collections.Generic;

public class Ally : BaseCharacter, IUseFsm, IUpdateListener
{
    [Header("Ally Configuration")] [SerializeField]
    private AllyDataSO allyData;

    private Transform playerToFollow;

    [Header("Guard Detection")] [Tooltip("Tag used to identify Guards")] [SerializeField]
    private string guardTag = "Guard";

    [Tooltip("Layer mask for Guard detection")] [SerializeField]
    private LayerMask guardLayerMask = 1 << 7;

    [Header("State Machine Configuration")] [SerializeField]
    private List<StateData> stateDataList = new List<StateData>();

    [SerializeField] private bool useFSM = true;

    // Configuration from AllyDataSO
    private float followDistance;
    private float followSpeed;
    private float attackRange;
    private float chaseSpeed;
    private float detectionRange;
    private BulletData bulletData;

    // Steering physics from AllyDataSO
    private float mass;
    private float maxForce;
    private float maxSpeed;
    private float slowingDistance;

    // Obstacle avoidance from AllyDataSO
    private LayerMask obstaclesMask;
    private float avoidRadius;
    private float avoidAngle;
    private float personalArea;
    private bool useFlocking;
    private float baseForceWeight;
    private float flockForceWeight;

    // Runtime state
    private Vector3 velocity;
    private Guard currentTarget;
    private Vector3 playerVelocity;
    private ObstacleAvoidance obstacleAvoidance;
    private FlockingSystem.FlockingEntity flockingEntity;
    private Vector3 lastKnownGuardPosition;
    private float lastTimeSawGuard = Mathf.NegativeInfinity;
    [SerializeField] private float loseGuardDelay = 0.6f;
    [SerializeField] private float coverReenterCooldown = 1.5f;
    private float lastTimeTookCover = Mathf.NegativeInfinity;
    private Vector3 coverPoint;
    private bool hasCoverPoint;
    private Collider lastCoverCollider;
    private Vector3 investigationTarget;
    private bool investigationComplete;
    private float investigationRotationRemaining;
    private bool investigationAtLocation;
    private GameObject smokeInstance;
    private float smokeEndTime;
    private float lastSmokeTime = Mathf.NegativeInfinity;
    private float coverLockUntil = Mathf.NegativeInfinity;

    [Header("AI Components (assign via Inspector if possible)")] [SerializeField]
    private AIContext aiContext;

    [SerializeField] private PlayerDetector playerDetectorComponent;

    private IPlayerDetector playerDetector;
    private float rotationSpeed = 3f;
    private StateMachine stateMachine;
    private float stateTimer;

    // Leader override system
    private bool leaderOverrideActive;
    private Vector3 leaderOverrideTarget;
    private float leaderOverrideExpiresAt;
    private string leaderOverrideRole;
    private Object leaderOverrideOwner;
    private int leaderOverridePriority;
    [SerializeField] private bool showStateLabel = true;

    // Pool service
    private static IPoolObjectsService PoolObjectsService => ServiceLocator.Get<IPoolObjectsService>();

    protected override void Awake()
    {
        base.Awake();

        InitializeFromAllyData();

        // Initialize steering
        velocity = Vector3.zero;
        obstacleAvoidance = new ObstacleAvoidance(transform, avoidRadius, avoidAngle, personalArea, obstaclesMask);
        flockingEntity = GetComponent<FlockingSystem.FlockingEntity>();

        // Shared AI context + detector (only from Inspector/explicit references)
        playerDetector = aiContext?.GetPlayerDetector() ?? playerDetectorComponent;
        if (playerDetector != null)
        {
            ConfigureDetectorForGuards();
            detectionRange = playerDetector.GetDetectionConfig().detectionRange;
        }
        else
        {
            Debug.LogWarning($"[Ally] {name} - PlayerDetector not assigned; detection will be disabled.");
        }

        InitializeFSM();

        // Subscribe to UpdateService
        SubscribeUpdateService();

        Debug.Log($"[Ally] {name} initialized - Independent from Guard, using AllyDataSO");
    }

    private void InitializeFromAllyData()
    {
        if (allyData != null)
        {
            // Player following
            followDistance = allyData.followDistance;
            followSpeed = allyData.followSpeed;
            useFlocking = allyData.useFlocking;
            baseForceWeight = allyData.followPlayerWeight;
            flockForceWeight = allyData.flockingWeight;

            // Combat
            attackRange = allyData.attackRange;
            chaseSpeed = allyData.chaseSpeed;
            bulletData = allyData.bulletData;

            // Detection (use SO value)
            detectionRange = allyData.detectionRange;

            // Steering physics
            mass = allyData.mass;
            maxForce = allyData.maxForce;
            maxSpeed = allyData.maxSpeed;
            slowingDistance = allyData.slowingDistance;

            // Obstacle avoidance
            obstaclesMask = allyData.obstaclesMask;
            avoidRadius = allyData.avoidRadius;
            avoidAngle = allyData.avoidAngle;
            personalArea = allyData.personalArea;

            // Rotation speed (use NPCDataSO value)
            rotationSpeed = allyData.rotationSpeed;

            Debug.Log($"[Ally] {name} loaded configuration from AllyDataSO");
        }
        else
        {
            MyLogger.LogError("Config the guard SO");
        }
    }

    private void Start()
    {
        // Auto-detect player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerToFollow = player.transform;
            Debug.Log($"[Ally] {name} found Player: {playerToFollow.name}");
        }
        else
        {
            Debug.LogError($"[Ally] {name} could not find Player with tag 'Player'!");
        }
    }

    private void OnDisable()
    {
        UnsubscribeUpdateService();
    }

    private void OnEnable()
    {
        // When reused from pool, re-subscribe to updates so movement/combat run again.
        SubscribeUpdateService();
    }

    #region IUpdateListener Implementation

    public virtual void MyUpdate()
    {
        if (!isAlive) return;

        UpdatePlayerVelocity();

        if (currentTarget != null && CanSeeGuard(currentTarget))
        {
            lastTimeSawGuard = Time.time;
        }

        if (HandleLeaderOverride())
        {
            currentTarget = null;
            return;
        }

        if (useFSM && stateMachine != null)
        {
            stateMachine.RunStateMachine();
            return;
        }

        UpdateBehavior();
        stateTimer += Time.deltaTime;
    }

    public void SubscribeUpdateService()
    {
        var updateService = ServiceLocator.Get<IUpdateService>();
        if (updateService != null)
        {
            updateService.AddUpdateListener(this);
        }
        else
        {
            Debug.LogWarning($"[Ally] {name} - UpdateService not found!");
        }
    }

    public void UnsubscribeUpdateService()
    {
        var updateService = ServiceLocator.Get<IUpdateService>();
        if (updateService != null)
        {
            updateService.RemoveUpdateListener(this);
        }
    }

    #endregion

    public void ResetFromPool()
    {
        ClearLeaderOverride(null, true);
        currentTarget = null;
        velocity = Vector3.zero;
        stateTimer = 0f;
        isAlive = true;
        lastShootTime = 0f;
        lastKnownGuardPosition = Vector3.zero;
        ClearCoverPoint();
        investigationTarget = Vector3.zero;
        investigationComplete = false;
        investigationAtLocation = false;
        investigationRotationRemaining = 0f;
        stateMachine?.ResetStateMachine();
    }

    #region Core Behavior

    private void UpdatePlayerVelocity()
    {
        if (playerToFollow == null) return;

        Rigidbody rb = playerToFollow.GetComponent<Rigidbody>();
        if (rb != null)
            playerVelocity = rb.linearVelocity;
    }

    private void UpdateBehavior()
    {
        // Priority 0: Leader Override (highest priority)
        if (HandleLeaderOverride())
        {
            currentTarget = null;
            return;
        }

        // Priority 1: Attack Guards if detected
        Guard detectedGuard = AcquireGuardTarget();
        if (detectedGuard != null)
        {
            if (currentTarget != detectedGuard)
            {
                Debug.Log($"[Ally] {name} encontro al guard {detectedGuard.name}");
            }

            currentTarget = detectedGuard;
            AttackGuard(detectedGuard);
            return;
        }

        // Priority 2: Follow Player
        currentTarget = null;
        FollowPlayer();
    }

    #endregion

    #region Combat

    public void AttackGuard(Guard target)
    {
        if (target == null || !target.IsAlive) return;

        lastKnownGuardPosition = target.transform.position;
        float distance = Vector3.Distance(transform.position, target.transform.position);

        // Movement: Pursue if far, brake if close
        if (distance > attackRange)
        {
            Vector3 targetVelocity = target.CurrentVelocity;
            Vector3 steeringForce = Steering.Pursuit(
                transform.position,
                velocity,
                target.transform.position,
                targetVelocity,
                chaseSpeed
            );
            ApplySteering(steeringForce);
        }
        else
        {
            // Brake
            Vector3 brakeForce = -velocity * 0.5f;
            ApplySteering(brakeForce);
        }

        // Shooting: Fire if in range
        if (distance <= attackRange && CanShoot())
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            Shoot(direction);
        }

        // Rotation: Face target
        Vector3 toTarget = (target.transform.position - transform.position).normalized;
        FaceDirection(toTarget);

        // Debug
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[Ally] {name} ATTACKING {target.name} at distance {distance:F2}");
        }
    }

    public Guard AcquireGuardTarget()
    {
        if (playerDetector == null)
            return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, guardLayerMask);

        Guard nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Guard guard = hit.GetComponent<Guard>();
            if (guard == null || !guard.IsAlive) continue;
            if (guard is Ally) continue;

            float distance = Vector3.Distance(transform.position, guard.transform.position);
            if (distance > minDistance) continue;

            if (!playerDetector.CanSeePlayer(guard.transform)) continue;

            minDistance = distance;
            nearest = guard;
        }

        currentTarget = nearest;
        if (nearest != null)
        {
            lastKnownGuardPosition = nearest.transform.position;
            lastTimeSawGuard = Time.time;
        }

        return nearest;
    }

    public bool CanSeeGuard(Guard guard)
    {
        if (guard == null) return false;

        if (playerDetector != null)
        {
            bool canSee = playerDetector.CanSeePlayer(guard.transform);
            if (canSee)
            {
                lastTimeSawGuard = Time.time;
            }

            return canSee;
        }

        float distance = Vector3.Distance(transform.position, guard.transform.position);
        bool inRange = distance <= detectionRange;
        if (inRange)
        {
            lastTimeSawGuard = Time.time;
        }

        return inRange;
    }

    public bool IsGuardInAttackRange()
    {
        if (currentTarget == null) return false;
        return Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange;
    }

    public bool IsGuardOutOfAttackRange(float tolerance = 0.2f)
    {
        if (currentTarget == null) return false;
        float threshold = attackRange * (1f + tolerance);
        return Vector3.Distance(transform.position, currentTarget.transform.position) > threshold;
    }

    public void ChaseCurrentGuard()
    {
        if (currentTarget == null || !currentTarget.IsAlive) return;

        Vector3 targetVelocity = currentTarget.CurrentVelocity;
        Vector3 steeringForce = Steering.Pursuit(
            transform.position,
            velocity,
            currentTarget.transform.position,
            targetVelocity,
            chaseSpeed
        );
        ApplySteering(steeringForce);
    }

    public bool ShouldFlock()
    {
        if (!useFlocking || flockingEntity == null || !isAlive)
            return false;

        if (LeaderOverrideActive)
            return false;

        // Avoid flocking when stationary attacking at close range to prevent jitter
        if (currentTarget != null && IsGuardInAttackRange())
            return false;

        return true;
    }

    /// <summary>
    /// Retarget the detector so it looks for Guards instead of the default Player tag/layer.
    /// </summary>
    private void ConfigureDetectorForGuards()
    {
        var detectorType = playerDetector.GetType();
        var tagField = detectorType.GetField("playerTag",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        var layerField = detectorType.GetField("playerLayerMask",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        tagField?.SetValue(playerDetector, guardTag);
        layerField?.SetValue(playerDetector, guardLayerMask);
    }

    #endregion

    #region Investigation / Search Helpers

    public Vector3 LastKnownGuardPosition
    {
        get => lastKnownGuardPosition;
        set => lastKnownGuardPosition = value;
    }

    public Vector3 InvestigationTarget => investigationTarget;
    public bool InvestigationComplete => investigationComplete;
    public bool InvestigationAtLocation => investigationAtLocation;
    public float InvestigationMoveSpeedFactor => allyData != null ? allyData.investigationMoveSpeedFactor : 0.7f;
    public float InvestigationArrivalTolerance => allyData != null ? allyData.investigationArrivalTolerance : 1.2f;
    public float LastTimeSawGuard => lastTimeSawGuard;
    public float LoseGuardDelay => loseGuardDelay;
    public float CoverReenterCooldown => coverReenterCooldown;

    public float LastTimeTookCover
    {
        get => lastTimeTookCover;
        set => lastTimeTookCover = value;
    }

    public float SearchDuration => allyData != null ? allyData.searchDuration : 4f;

    public void BeginInvestigation(Vector3 targetPosition)
    {
        investigationTarget = targetPosition;
        investigationComplete = false;
        investigationAtLocation = false;
        investigationRotationRemaining = 360f;
        stateTimer = 0f;
        if (targetPosition != Vector3.zero)
        {
            lastKnownGuardPosition = targetPosition;
        }
    }

    public void MarkInvestigationArrived()
    {
        investigationAtLocation = true;
    }

    public void CompleteInvestigation()
    {
        investigationComplete = true;
        investigationAtLocation = false;
        investigationRotationRemaining = 0f;
    }

    public void StepInvestigationScan()
    {
        if (investigationComplete) return;

        float rotateAmount = (allyData != null ? allyData.investigationRotateSpeed : 180f) * Time.deltaTime;
        investigationRotationRemaining -= rotateAmount;

        transform.Rotate(0f, rotateAmount, 0f);

        if (investigationRotationRemaining <= 0f)
        {
            CompleteInvestigation();
        }
    }

    public void MoveTowardsPoint(Vector3 target, float speed, float arrivalTolerance)
    {
        if (target == Vector3.zero) return;

        Vector3 steering = Steering.Arrive(
            transform.position,
            target,
            velocity,
            speed,
            slowingDistance);

        float distance = Vector3.Distance(transform.position, target);
        if (distance <= arrivalTolerance)
        {
            velocity = Vector3.zero;
            return;
        }

        ApplySteering(steering);
    }

    public void StopMovement()
    {
        velocity = Vector3.zero;
    }

    public void FaceDirectionTowards(Vector3 direction)
    {
        FaceDirection(direction);
    }

    #endregion

    #region Cover Helpers

    public bool HasCoverPoint => hasCoverPoint;
    public Vector3 CoverPoint => coverPoint;
    public float CoverOffsetFromObstacle => allyData != null ? allyData.coverOffsetFromObstacle : 1.25f;
    public float CoverProbeRadius => allyData != null ? allyData.coverProbeRadius : 0.9f;
    public float CoverProbeDistance => allyData != null ? allyData.coverProbeDistance : 4f;
    public float CoverRepositionCooldown => allyData != null ? allyData.coverRepositionCooldown : 1.2f;
    public float CoverArrivalTolerance => allyData != null ? allyData.coverArrivalTolerance : 0.9f;
    public float CoverHealthRegenPerSecond => allyData != null ? allyData.coverHealthRegenPerSecond : 10f;
    public float CoverExitHealthPercent => allyData != null ? allyData.coverExitHealthPercent : 0.75f;
    public float CoverMinDuration => allyData != null ? allyData.coverMinDuration : 1.5f;
    public Collider LastCoverCollider => lastCoverCollider;
    public AllyDataSO AllyData => allyData;

    public float LastSmokeTime
    {
        get => lastSmokeTime;
        set => lastSmokeTime = value;
    }

    public GameObject SmokeInstance
    {
        get => smokeInstance;
        set => smokeInstance = value;
    }

    public bool CoverLockActive => Time.time < coverLockUntil;

    public void StartCoverLock(float duration)
    {
        coverLockUntil = Time.time + Mathf.Max(0f, duration);
    }

    public void SetCoverPoint(Vector3 point)
    {
        coverPoint = point;
        hasCoverPoint = true;
    }

    public void ClearCoverPoint()
    {
        hasCoverPoint = false;
        coverPoint = Vector3.zero;
        lastCoverCollider = null;
        coverLockUntil = Mathf.NegativeInfinity;
    }

    public void SetCoverDebug(Collider collider, Vector3 hitPoint, Vector3 hitNormal)
    {
        lastCoverCollider = collider;
    }

    public void RegenerateHealth(float amount)
    {
        if (!isAlive || amount <= 0f) return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
    }

    #endregion

    #region Player Following

    public void FollowPlayer()
    {
        if (playerToFollow == null)
        {
            // Fallback: intenta reasignar el Player si no está seteado
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerToFollow = player.transform;
                Debug.Log($"[Ally] {name} re-found Player: {playerToFollow.name}");
            }

            if (playerToFollow == null && Time.frameCount % 60 == 0)
                Debug.LogWarning($"[Ally] {name} has no Player to follow!");
            return;
        }

        float distance = Vector3.Distance(transform.position, playerToFollow.position);
        float outerResume = followDistance + FollowDistanceBuffer;
        float innerStop = Mathf.Max(0f, followDistance - FollowDistanceBuffer);

        // Movement: Pursue if far, brake if close
        if (distance > outerResume)
        {
            Vector3 steeringForce = Steering.Pursuit(
                transform.position,
                velocity,
                playerToFollow.position,
                playerVelocity,
                followSpeed
            );
            ApplySteering(steeringForce);
        }
        else if (distance < innerStop)
        {
            // Brake gently
            Vector3 brakeForce = -velocity * 0.3f;
            ApplySteering(brakeForce);
        }

        Vector3 lookDir = velocity.sqrMagnitude > 0.01f
            ? velocity
            : (playerToFollow.position - transform.position);
        FaceDirection(lookDir);

        // Debug
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Ally] {name} FOLLOWING Player at distance {distance:F2}");
        }
    }

    #endregion

    #region Steering Physics (Copied from Guard)

    /// <summary>
    /// Apply steering force with physics integration and obstacle avoidance.
    /// </summary>
    private void ApplySteering(Vector3 steeringForce)
    {
        if (!isAlive) return;

        Vector3 finalSteering = steeringForce;

        if (useFlocking && flockingEntity != null && ShouldFlock())
        {
            Vector3 flockForce = flockingEntity.GetFlockingForce();
            finalSteering = (steeringForce * baseForceWeight) + (flockForce * flockForceWeight);
        }

        // 1. Integrate steering force into velocity
        Vector3 desiredVelocity = Integrate(finalSteering, Time.deltaTime);
        desiredVelocity.y = 0f; // Keep on ground

        float desiredSpeed = desiredVelocity.magnitude;
        if (desiredSpeed <= 0.0001f) return;

        Vector3 desiredDirection = desiredVelocity / Mathf.Max(desiredSpeed, 1e-5f);

        // 2. Apply obstacle avoidance
        Vector3 avoidedVelocity = obstacleAvoidance.GetDirImproved(desiredVelocity, false);
        Vector3 avoidanceDelta = avoidedVelocity - desiredVelocity;
        Vector3 avoidDirection = avoidanceDelta.sqrMagnitude > 1e-6f ? avoidanceDelta.normalized : Vector3.zero;

        // Blend desired movement with avoidance
        float pathWeight = 1.0f;
        float avoidWeight = 0.35f;

        if (avoidDirection != Vector3.zero)
        {
            float oppositeFactor = Mathf.Clamp01(-Vector3.Dot(avoidDirection, desiredDirection));
            float weightBoost = Mathf.Lerp(0f, 0.75f, oppositeFactor);
            avoidWeight += weightBoost;
        }

        avoidWeight = Mathf.Clamp(avoidWeight, 0f, 1f);

        float avoidScale = Mathf.Max(desiredSpeed, 0.1f);
        Vector3 blendedVelocity = (desiredVelocity * pathWeight) + (avoidDirection * (avoidWeight * avoidScale));

        // Clamp to max speed while prioritizing path component
        if (blendedVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            Vector3 pathComponent = Vector3.Project(blendedVelocity, desiredDirection);
            Vector3 avoidComponent = blendedVelocity - pathComponent;

            float pathMag = pathComponent.magnitude;
            float avoidMag = avoidComponent.magnitude;
            float totalMag = Mathf.Sqrt(pathMag * pathMag + avoidMag * avoidMag);

            if (totalMag > maxSpeed)
            {
                float scale = Mathf.Sqrt(Mathf.Max(0, maxSpeed * maxSpeed - pathMag * pathMag)) /
                              Mathf.Max(avoidMag, 1e-5f);
                avoidComponent *= Mathf.Min(scale, 1f);
                blendedVelocity = pathComponent + avoidComponent;
            }
        }

        blendedVelocity.y = 0f;
        velocity = blendedVelocity;

        // 3. Apply movement
        if (velocity.sqrMagnitude > 0.001f)
        {
            Vector3 movement = velocity * Time.deltaTime;
            transform.position += movement;
        }
    }

    /// <summary>
    /// Integrate steering force with mass and force limits.
    /// </summary>
    private Vector3 Integrate(Vector3 steeringForce, float deltaTime)
    {
        // Clamp steering force to maximum
        Vector3 clampedForce = steeringForce;
        if (clampedForce.sqrMagnitude > maxForce * maxForce)
        {
            clampedForce = clampedForce.normalized * maxForce;
        }

        // Apply force to velocity (F = ma, so a = F/m)
        Vector3 acceleration = clampedForce / mass;
        Vector3 newVelocity = velocity + acceleration * deltaTime;

        // Clamp velocity to maximum speed
        if (newVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            newVelocity = newVelocity.normalized * maxSpeed;
        }

        return newVelocity;
    }

    /// <summary>
    /// Smoothly rotate to face a direction.
    /// </summary>
    private void FaceDirection(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;

        direction.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Abstract Method Implementations

    public override void Move(Vector3 direction)
    {
        // Direct movement (used for legacy compatibility)
        Vector3 targetVelocity = direction.normalized * maxSpeed;
        Vector3 steeringForce = targetVelocity - velocity;
        ApplySteering(steeringForce);
    }

    public override void Shoot(Vector3 direction)
    {
        if (!isAlive || !CanShoot()) return;

        lastShootTime = Time.time;
        CreateBullet(direction);
    }

    private void CreateBullet(Vector3 direction)
    {
        if (bulletData == null)
        {
            Debug.LogWarning($"[Ally] {name} has no BulletData assigned!");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f + direction * 0.8f;
        var bullet = PoolObjectsService.GetOrCreateObject(bulletData.Prefab);
        bullet.OnDeactivate += OnBulletDeactivate;
        bullet.InitializeBullet(bulletData, spawnPosition, direction, BulletOwner.Ally, gameObject.name);
    }

    private void OnBulletDeactivate(BulletObject bullet)
    {
        bullet.OnDeactivate -= OnBulletDeactivate;
        PoolObjectsService.ReturnObject(bullet);
    }

    #endregion

    #region Leader Override System

    /// <summary>
    /// Set a leader override command that takes priority over normal behavior.
    /// Used by AllyLeader to coordinate defensive formations and tactics.
    /// </summary>
    public void SetLeaderOverride(Vector3 target, float duration, string role = "", UnityEngine.Object owner = null,
        int priority = 0)
    {
        if (leaderOverrideActive
            && leaderOverrideOwner != null
            && owner != null
            && owner != leaderOverrideOwner
            && priority < leaderOverridePriority)
        {
            Debug.Log(
                $"[Ally] {name} override rejected by {owner} (prio {priority}) because active owner {leaderOverrideOwner} has prio {leaderOverridePriority}");
            return;
        }

        leaderOverrideActive = true;
        leaderOverrideTarget = target;
        leaderOverrideExpiresAt = Time.time + duration;
        leaderOverrideRole = role;
        leaderOverrideOwner = owner;
        leaderOverridePriority = priority;

        Debug.Log(
            $"[Ally] {name} received leader override: {role} at {target} for {duration}s (owner {owner}, prio {priority})");
    }

    /// <summary>
    /// Clear the current leader override, returning to normal behavior.
    /// </summary>
    public void ClearLeaderOverride(Object requester = null, bool force = false)
    {
        if (leaderOverrideOwner != null && requester != null && requester != leaderOverrideOwner && !force)
        {
            Debug.Log($"[Ally] {name} clear ignored by {requester} (owner {leaderOverrideOwner})");
            return;
        }

        leaderOverrideActive = false;
        leaderOverrideTarget = Vector3.zero;
        leaderOverrideRole = string.Empty;
        leaderOverrideExpiresAt = 0f;
        leaderOverrideOwner = null;
        leaderOverridePriority = 0;

        Debug.Log($"[Ally] {name} cleared leader override (by {requester})");
    }

    /// <summary>
    /// Handle leader override behavior. Returns true if override is active and handled.
    /// </summary>
    private bool HandleLeaderOverride()
    {
        if (!leaderOverrideActive)
            return false;

        // If regroup/escort override is active but a guard is detected, drop override to engage
        if (!string.IsNullOrEmpty(leaderOverrideRole)
            && leaderOverrideRole.ToUpperInvariant() == "REGROUP")
        {
            Guard threat = AcquireGuardTarget();
            if (threat != null && threat.IsAlive)
            {
                ClearLeaderOverride();
                return false; // allow normal combat logic
            }
        }

        // Check if override expired
        if (Time.time >= leaderOverrideExpiresAt)
        {
            ClearLeaderOverride();
            return false;
        }

        // Move toward override target
        float distance = Vector3.Distance(transform.position, leaderOverrideTarget);

        float arrivalTolerance = Mathf.Max(0.5f, followDistance * 0.5f); // evita ping-pong cercano

        if (distance > arrivalTolerance)
        {
            // Use Seek behavior to move to target
            Vector3 steeringForce = Steering.Seek(
                transform.position,
                velocity,
                leaderOverrideTarget,
                followSpeed
            );
            ApplySteering(steeringForce);
        }
        else
        {
            // Reached target, hard brake to evitar oscilaciones
            velocity = Vector3.zero;
        }

        // Face the override target
        Vector3 toTarget = (leaderOverrideTarget - transform.position).normalized;
        if (toTarget.magnitude > 0.1f)
        {
            FaceDirection(toTarget);
        }

        // Debug
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[Ally] {name} executing override '{leaderOverrideRole}' - distance: {distance:F2}");
        }

        return true;
    }

    #endregion

    #region FSM Bootstrapping

    private void InitializeFSM()
    {
        if (useFSM && stateDataList != null && stateDataList.Count > 0)
        {
            stateMachine = new StateMachine(stateDataList, this);
            MyLogger.LogInfo($"[Ally] {name}: FSM inicializada con {stateDataList.Count} estados");
        }
        else
        {
            MyLogger.LogWarning(
                $"[Ally] {name}: FSM no inicializada - useFSM: {useFSM}, states: {stateDataList?.Count ?? 0}");
        }
    }

    public void UpdateFsm()
    {
        stateMachine?.RunStateMachine();
    }

    #endregion

    #region Public Accessors

    protected string GetCurrentStateName() => stateMachine?.GetCurrentState()?.State?.StateName ?? "None";
    public Transform GetModelTransform() => transform;
    public Transform GetTargetTransform() => currentTarget != null ? currentTarget.transform : null;

    public void SetTargetTransform(Transform target)
    {
        currentTarget = target != null ? target.GetComponent<Guard>() : null;
    }

    public Guard GetCurrentTarget() => currentTarget;
    public float CurrentHealthPercent => MaxHealth > 0.01f ? CurrentHealth / MaxHealth : 0f;
    public bool IsLowHealth(float threshold) => CurrentHealthPercent <= Mathf.Clamp01(threshold);
    public Transform GetPlayerToFollow() => playerToFollow;
    public void SetPlayerToFollow(Transform player) => playerToFollow = player;
    public float AttackRange => attackRange;
    public float FollowSpeed => followSpeed;
    public LayerMask ObstaclesMask => obstaclesMask;

    public float StateTimer
    {
        get => stateTimer;
        set => stateTimer = value;
    }

    public bool LeaderOverrideActive => leaderOverrideActive;
    public float FollowDistanceBuffer => allyData != null ? allyData.followDistanceBuffer : 2f;

    #endregion

    #region Debug Gizmos

    protected virtual void OnDrawGizmosSelected()
    {
        if (showStateLabel)
        {
            FsmGizmoHelper.DrawStateLabel(transform, $"State: {GetCurrentStateName()}", Color.cyan, 2f);
        }

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Follow distance
        if (playerToFollow != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerToFollow.position, followDistance);
            Gizmos.DrawLine(transform.position, playerToFollow.position);
        }

        // Current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Leader override target
        if (leaderOverrideActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leaderOverrideTarget, 1.5f);
            Gizmos.DrawLine(transform.position, leaderOverrideTarget);

#if UNITY_EDITOR
            // Draw role text in editor
            UnityEditor.Handles.Label(
                leaderOverrideTarget + Vector3.up * 2f,
                $"Override: {leaderOverrideRole}",
                new GUIStyle { normal = new GUIStyleState { textColor = Color.cyan } }
            );
#endif
        }
    }
} 

#endregion
