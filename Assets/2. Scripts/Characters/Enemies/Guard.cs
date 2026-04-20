using System;
using UnityEngine;
using Game.AI.Steering;
using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using System.Collections.Generic;
using ScriptableObjects.Bullets;
using Services.MicroServices.BlackboardService;
using Services;
using Services.MicroServices.PoolObjectsService;
using Services.MicroServices.UpdateService;
using Unity.Assertions;

//TODO: pasar a MVP
public class Guard : BaseCharacter, IUseFsm, IUpdateListener
{
    //todo utilizar scriptable object
    [Header("Guard Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float fieldOfView = 90f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float idleTime = 3f;
    [SerializeField] private float searchTime = 5f;
    [SerializeField] private float baseRotationSpeed = 2f;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private BulletData bulletData;

    [Header("Targeting")]
    [SerializeField] protected string[] targetTags = new[] { "Player", "Ally" };

    [Header("FSM Patrol Settings")]
    [SerializeField] private int loopsToIdle = 3;
    [SerializeField] private float idleSeconds = 5f;

    [Header("State Machine Configuration")]
    [SerializeField] private List<StateData> stateDataList = new List<StateData>();
    [SerializeField] private bool useFSM = true;
    
    [Header("AI Configuration")]
    [SerializeField] private AIPersonalityType personalityType = AIPersonalityType.Aggressive;
    [SerializeField] private bool enableNewAISystem = true;

    [Header("Flocking Integration")]
    [SerializeField] private bool useFlocking = false;
    [SerializeField] private float baseForceWeight = 0.8f;
    [SerializeField] private float flockForceWeight = 0.2f;

    [Header("Steering Physics")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float slowingDistance = 2f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstaclesMask = -1;
    [SerializeField] private float avoidRadius = 2f;
    [SerializeField] private float avoidAngle = 90f;
    [SerializeField] private float personalArea = 0.5f;

    [Header("Cover Behavior")]
    [SerializeField] private float coverProbeRadius = 0.9f;
    [SerializeField] private float coverProbeDistance = 4f;
    [SerializeField] private float coverOffsetFromObstacle = 1.25f;
    [SerializeField] private float coverRepositionCooldown = 1.2f;
    [SerializeField] private float losePlayerTimeout = 4f;

    [Header("Investigation State")]
    [SerializeField] private float investigationRotateSpeed = 180f;
    [SerializeField] private float investigationMoveSpeedFactor = 0.7f;
    [SerializeField] private float investigationArrivalTolerance = 1.2f;

    [Header("Reinforcement/Smoke")]
    [SerializeField] private float damageRecentWindow = 3f;
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private float smokeLifetime = 5f;
    [SerializeField] private float smokeScale = 3f;
    [SerializeField] private string obstacleLayerName = "ObstacleAI";

    [Header("Leader Override")]
    [SerializeField] private float overrideArrivalTolerance = 1.5f;
    [SerializeField] private float overrideDuration = 8f;
    [SerializeField] private float overrideDwellTime = 2f;

    [Header("Detain/Knockout")]
    [SerializeField] private float knockoutRecoverTime = 3f;
    [SerializeField] private float detainRange = 2.5f;
    [SerializeField] private float detainBackAngle = 120f;
    [SerializeField] private float detainDamageMultiplier = 2f;
    [SerializeField] private bool ignoreLeaderDuringDetain = true;
    [SerializeField] private float detainMoveSpeedFactor = 0.6f;
    [Header("Debug")]
    [SerializeField] protected bool showStateLabel = true;

    [Header("Health Regeneration")]
    [SerializeField] private bool enableHealthRegen = true;
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private float regenRate = 4f;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.35f;
    [SerializeField, Range(0f, 1f)] private float recoveredHealthThreshold = 0.8f;
    
    private Transform player;
    private BaseCharacter targetCharacter;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex;
    private float stateTimer;
    private bool isActivelyPatrolling;  // Track patrol state independently

    // FSM patrol tracking
    private int currentPatrolLoops = 0;
    private bool patrolDirection = true; // true = 0->N, false = N->0
    private bool hasReachedCurrentPatrolPoint = false;
    
    // AI System components
    private AIContext aiContext;
    private IBlackboardService m_blackboardService;
    private IPlayerDetector playerDetector;

    // Steering components
    private Vector3 _vel;
    private ObstacleAvoidance obstacleAvoidance;
    private FlockingSystem.FlockingEntity flockingEntity;

    // FSM components
    private StateMachine stateMachine;
    
    // Movement state for IAIMovementController
    private Vector3 currentMovementDirection;
    private float currentMovementSpeed;
    private Vector3 currentDestination;
    private MovementMode currentMovementMode = MovementMode.Walk;
    private MovementStatus currentMovementStatus = MovementStatus.Idle;
    private bool isMovementPaused = false;
    private Transform steeringTarget;
    private Bounds movementConstraints;
    private bool hasMovementConstraints = false;
    private float lastDamageTime = Mathf.NegativeInfinity;
    private float lastTimeSawPlayer = Mathf.NegativeInfinity;
    private Vector3 currentCoverPoint;
    private bool hasCoverPoint;
    private Collider lastCoverCollider;
    private Vector3 lastCoverHitPoint;
    private Vector3 lastCoverHitNormal;
    private GameObject smokeInstance;
    private float smokeEndTime;
    private bool investigationComplete;
    private float investigationRotationRemaining;
    private bool investigationAtLocation;
    private Vector3 investigationTarget;
    private bool leaderOverrideActive;
    private Vector3 leaderOverrideTarget;
    private float leaderOverrideExpiresAt;
    private float leaderOverrideReachedAt;
    private string leaderOverrideRole;
    private bool isKnockedOut;
    private float knockoutTimer;
    private bool isDetaining;
    private Transform detainTarget;
    private float detainTimer;
    private float detainMaxDuration = 5f;
    private UnityEngine.Object leaderOverrideOwner;
    private int leaderOverridePriority;
    
    // Callbacks
    public Action OnMovementComplete { get; set; }
    public Action OnMovementBlocked { get; set; }
    
    public float AttackRange => attackRange;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float IdleTime => idleTime;
    public float SearchTime => searchTime;
    public float BaseRotationSpeed => baseRotationSpeed;
    public Transform[] PatrolPoints => patrolPoints;
    public Transform Player => player;
    public Vector3 LastKnownPlayerPosition
    {
        get => lastKnownPlayerPosition;
        set => lastKnownPlayerPosition = value;
    }
    public int CurrentPatrolIndex
    {
        get => currentPatrolIndex;
        set => currentPatrolIndex = value;
    }
    public float StateTimer
    {
        get => stateTimer;
        set => stateTimer = value;
    }

    // FSM Patrol Properties
    public int LoopsToIdle => loopsToIdle;
    public float IdleSeconds => idleSeconds;
    public int CurrentPatrolLoops
    {
        get => currentPatrolLoops;
        set => currentPatrolLoops = value;
    }
    public bool PatrolDirection
    {
        get => patrolDirection;
        set => patrolDirection = value;
    }
    public bool HasReachedCurrentPatrolPoint
    {
        get => hasReachedCurrentPatrolPoint;
        set => hasReachedCurrentPatrolPoint = value;
    }
    
    public bool IsActive => isAlive && gameObject.activeInHierarchy;
    
    // AI System access
    public IBlackboardService BlackboardService => m_blackboardService;

    // Steering Physics access
    public float MaxForce => maxForce;
    public float SlowingDistance => slowingDistance;
    public Vector3 CurrentVelocity => _vel;
    public LayerMask ObstaclesMask => obstaclesMask;
    public float CoverProbeRadius => coverProbeRadius;
    public float CoverProbeDistance => coverProbeDistance;
    public float CoverOffsetFromObstacle => coverOffsetFromObstacle;
    public float CoverRepositionCooldown => coverRepositionCooldown;
    public float LosePlayerTimeout => losePlayerTimeout;
    public float HealthNormalized => MaxHealth <= 0f ? 0f : currentHealth / MaxHealth;
    public float TimeSinceLastSeenPlayer => Time.time - lastTimeSawPlayer;
    public bool RecentlySawPlayer(float window) => Time.time - lastTimeSawPlayer <= window;
    public Vector3 CoverPoint => currentCoverPoint;
    public bool HasCoverPoint => hasCoverPoint;
    public string CurrentStateName => stateMachine?.GetCurrentState()?.State?.StateName ?? "None";
    public Collider LastCoverCollider => lastCoverCollider;
    public Vector3 LastCoverHitPoint => lastCoverHitPoint;
    public Vector3 LastCoverHitNormal => lastCoverHitNormal;
    public float InvestigationRotateSpeed => investigationRotateSpeed;
    public float InvestigationMoveSpeedFactor => investigationMoveSpeedFactor;
    public float InvestigationArrivalTolerance => investigationArrivalTolerance;
    public bool InvestigationComplete => investigationComplete;
    public float InvestigationRotationRemaining
    {
        get => investigationRotationRemaining;
        set => investigationRotationRemaining = value;
    }
    public bool InvestigationAtLocation
    {
        get => investigationAtLocation;
        set => investigationAtLocation = value;
    }
    public Vector3 InvestigationTarget
    {
        get => investigationTarget;
        set => investigationTarget = value;
    }
    public bool TookDamageRecently(float window) => Time.time - lastDamageTime <= window;
    public bool LeaderOverrideActive => leaderOverrideActive;
    public bool IsKnockedOut => isKnockedOut;
    public bool IsDetaining => isDetaining;
    public Transform DetainTarget => detainTarget;
    public float KnockoutRecoverTime => knockoutRecoverTime;
    public float DetainRange => detainRange;
    public float DetainBackAngle => detainBackAngle;
    public float DetainMoveSpeedFactor => detainMoveSpeedFactor;
    
    private static IPoolObjectsService PoolObjectsService => ServiceLocator.Get<IPoolObjectsService>();

    public void SetCoverPoint(Vector3 coverPoint)
    {
        currentCoverPoint = coverPoint;
        hasCoverPoint = true;
    }

    public void ClearCoverPoint()
    {
        hasCoverPoint = false;
        currentCoverPoint = Vector3.zero;
        lastCoverCollider = null;
        lastCoverHitPoint = Vector3.zero;
        lastCoverHitNormal = Vector3.zero;
    }

    public void SetCoverDebug(Collider collider, Vector3 hitPoint, Vector3 hitNormal)
    {
        lastCoverCollider = collider;
        lastCoverHitPoint = hitPoint;
        lastCoverHitNormal = hitNormal;
    }

    public void TriggerKnockout(Transform attacker = null)
    {
        if (isKnockedOut || !isAlive) return;

        // Only allow knockout if attacker is behind and guard is in Idle/Patrol
        if (attacker != null)
        {
            Vector3 toGuard = (transform.position - attacker.position).normalized;
            float angle = Vector3.Angle(attacker.forward, toGuard);
            if (angle > detainBackAngle * 0.5f) return;
        }

        if (stateMachine != null)
        {
            var currState = stateMachine.GetCurrentState()?.State?.StateName;
            if (currState != null && currState != "Idle" && currState != "Patrol")
                return;
        }

        isKnockedOut = true;
        knockoutTimer = knockoutRecoverTime;
        ClearLeaderOverride();
        Debug.Log($"[Detain] {name} knocked out");
    }

    public void SetDetainTarget(Transform target)
    {
        if (target == null || !isAlive) return;
        detainTarget = target;
        isDetaining = true;
        detainTimer = detainMaxDuration;
        ClearLeaderOverride();
        Debug.Log($"[Detain] {name} detaining target {target.name}");
    }

    public void RecoverFromKnockout()
    {
        isKnockedOut = false;
        knockoutTimer = 0f;
    }

    public void StopDetaining()
    {
        isDetaining = false;
        detainTarget = null;
        detainTimer = 0f;
    }

    public void ApplyDetainDamage()
    {
        if (detainTarget == null) return;
        var victim = detainTarget.GetComponent<BaseCharacter>();
        if (victim != null)
        {
            float dmg = victim.MaxHealth * detainDamageMultiplier;
            victim.TakeDamage(dmg);
            Debug.Log($"[Detain] {name} applied detain damage {dmg} to {victim.name}");
        }
    }

    public void BeginInvestigation(Vector3 targetPosition)
    {
        investigationTarget = targetPosition;
        investigationComplete = false;
        investigationRotationRemaining = 360f;
        investigationAtLocation = false;
    }

    public void MarkInvestigationArrived()
    {
        investigationAtLocation = true;
    }

    public void CompleteInvestigation()
    {
        investigationComplete = true;
        investigationRotationRemaining = 0f;
    }

    public void DeploySmoke()
    {
        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        if (smokeInstance != null)
        {
            Destroy(smokeInstance);
        }

        if (smokePrefab != null)
        {
            smokeInstance = Instantiate(smokePrefab, transform.position, Quaternion.identity);
            smokeInstance.transform.localScale *= smokeScale;
        }
        else
        {
            smokeInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            smokeInstance.transform.position = transform.position;
            smokeInstance.transform.localScale = Vector3.one * smokeScale;

            var renderer = smokeInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        smokeInstance.layer = obstacleLayer;

        var collider = smokeInstance.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;

            // Evita empujar al guard que lo genera (y colliders hijos)
            var selfColliders = GetComponentsInChildren<Collider>();
            foreach (var selfCol in selfColliders)
            {
                if (selfCol != null && selfCol != collider)
                {
                    Physics.IgnoreCollision(collider, selfCol, true);
                }
            }
        }

        smokeEndTime = Time.time + smokeLifetime;
        Destroy(smokeInstance, smokeLifetime);
    }

    public void SetLeaderOverride(Vector3 target, float duration, string role = "", UnityEngine.Object owner = null, int priority = 0)
    {
        // Reject if another owner with higher priority is active
        if (leaderOverrideActive
            && leaderOverrideOwner != null
            && owner != null
            && owner != leaderOverrideOwner
            && priority < leaderOverridePriority)
        {
            Debug.Log($"[LeaderOverride] {name} override rejected by {owner} (prio {priority}) because active owner {leaderOverrideOwner} has prio {leaderOverridePriority}");
            return;
        }
        
        leaderOverrideActive = true;
        leaderOverrideTarget = target;
        leaderOverrideExpiresAt = Time.time + (duration > 0f ? duration : overrideDuration);
        leaderOverrideReachedAt = -1f;
        leaderOverrideRole = role;
        leaderOverrideOwner = owner;
        leaderOverridePriority = priority;
        
        MyLogger.LogInfo($"[LeaderOverride] {name} override set -> target {target}, duration {duration}, role {role}");
    }

    public void ClearLeaderOverride(UnityEngine.Object requester = null, bool force = false)
    {
        if (leaderOverrideOwner != null && requester != null && requester != leaderOverrideOwner && !force)
        {
            Debug.Log($"[LeaderOverride] {name} clear ignored by {requester} (owner {leaderOverrideOwner})");
            return;
        }
        
        leaderOverrideActive = false;
        leaderOverrideTarget = Vector3.zero;
        leaderOverrideRole = string.Empty;
        leaderOverrideExpiresAt = 0f;
        leaderOverrideReachedAt = -1f;
        leaderOverrideOwner = null;
        leaderOverridePriority = 0;
        
        MyLogger.LogInfo($"[LeaderOverride] {name} override cleared");
    }
    
    // MEJORA: Improved player detection using new AI system
    public bool CanSeePlayer()
    {
        if (enableNewAISystem && playerDetector != null)
        {
            return playerDetector.CanSeePlayer(player);
        }
        
        // Fallback to legacy detection
        return CanSeePlayerLegacy();
    }
    
    // MEJORA: Get advanced detection information
    public DetectionResult GetDetectionResult()
    {
        if (enableNewAISystem && playerDetector != null)
        {
            return playerDetector.GetCurrentDetectionResult();
        }
        
        return CanSeePlayerLegacy() ? DetectionResult.Clear : DetectionResult.None;
    }
    
    private bool CanSeePlayerLegacy()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        if (angleToPlayer > fieldOfView / 2f) return false;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return false;
        
        return !Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, LayerMask.GetMask("Obstacles"));
    }

    public override void Initialize()
    {
        base.Initialize();
        InitializeAISystem();
        SetupPatrolPoints();
        
        SubscribeUpdateService();
        
        // Start patrolling after a frame to ensure everything is initialized
        if (isActiveAndEnabled)
        {
            StartCoroutine(StartPatrolAfterFrame());
        }
    }

    private System.Collections.IEnumerator StartPatrolAfterFrame()
    {
        yield return null; // Wait one frame

        if (!isActiveAndEnabled) yield break;
        
        StartPatrol();
    }
    
    private void InitializeAISystem()
    {
        if (enableNewAISystem)
        {
            // Initialize AI Context
            aiContext = gameObject.GetComponent<AIContext>();
            Assert.IsNotNull(aiContext);

            // Get blackboard service
            m_blackboardService = ServiceLocator.Get<IBlackboardService>();
            if (m_blackboardService == null)
            {
                MyLogger.LogWarning($"Guard {gameObject.name}: Blackboard service not available yet");
            }

            // Get player detector
            playerDetector = gameObject.GetComponent<IPlayerDetector>();
            Assert.IsNotNull(playerDetector);

            // Configure personality
            if (aiContext != null)
            {
                aiContext.SetPersonalityType(personalityType);
            }
        }

        // Initialize steering physics
        _vel = Vector3.zero;
        obstacleAvoidance = new ObstacleAvoidance(transform, avoidRadius, avoidAngle, personalArea, obstaclesMask);

        // Initialize flocking entity if present
        if (useFlocking)
        {
            flockingEntity = GetComponent<FlockingSystem.FlockingEntity>();
            if (flockingEntity == null)
            {
                MyLogger.LogWarning($"Guard {gameObject.name}: useFlocking enabled but FlockingEntity component not found");
            }
        }

        // Ensure maxSpeed is at least as fast as chaseSpeed for proper movement
        if (maxSpeed < chaseSpeed)
        {
            maxSpeed = chaseSpeed * 1.2f; // Give some headroom
            MyLogger.LogInfo($"Guard {gameObject.name}: Adjusted maxSpeed to {maxSpeed} to match chaseSpeed");
        }

        // Initialize FSM
        InitializeFSM();
    }

    private void InitializeFSM()
    {
        if (useFSM && stateDataList != null && stateDataList.Count > 0)
        {
            stateMachine = new StateMachine(stateDataList, this);
            MyLogger.LogInfo($"Guard {gameObject.name}: FSM initialized with {stateDataList.Count} states");
        }
        else
        {
            MyLogger.LogWarning($"Guard {gameObject.name}: FSM not initialized - useFSM: {useFSM}, stateDataList count: {stateDataList?.Count ?? 0}");
        }
    }
    
    private void Start()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(DelayedStart());
        }
    }
    
    private System.Collections.IEnumerator DelayedStart()
    {
        yield return null;

        if (!isActiveAndEnabled) yield break;
        
        // Ensure blackboard connection is established
        if (enableNewAISystem && m_blackboardService == null)
        {
            m_blackboardService = ServiceLocator.Get<IBlackboardService>();
        }
        
        // Find player/target if not set
        if (player == null)
        {
            GameObject closest = null;
            float minDist = float.MaxValue;

            foreach (var tag in targetTags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
                if (candidates == null) continue;

                for (int i = 0; i < candidates.Length; i++)
                {
                    float d = Vector3.Distance(transform.position, candidates[i].transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = candidates[i];
                    }
                }
            }

            if (closest != null)
            {
                SetTargetTransform(closest.transform);
                MyLogger.LogInfo($"[Guard] Target assigned automatically by tags [{string.Join(",", targetTags)}]: {closest.name}");
            }
        }
    }
    
    private void UpdateMovementSystem(float deltaTime)
    {
        // Only log movement system updates when there are issues or state changes
        if (isMovementPaused || !CanMove()) 
        {
            // Only log once per second when blocked to avoid spam
            if (Time.frameCount % 60 == 0)
            {
                MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: Movement blocked - isPaused: {isMovementPaused}, CanMove: {CanMove()}");
            }
            return;
        }
        
        // Update steering target following
        if (steeringTarget != null && currentMovementStatus == MovementStatus.Following)
        {
            currentDestination = steeringTarget.position;
            Vector3 direction = (currentDestination - transform.position).normalized;
            currentMovementDirection = direction;
            // Only log steering updates occasionally
            if (Time.frameCount % 30 == 0)
            {
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Following steering target to {currentDestination}");
            }
        }
        
        // Debug patrol status only when reaching points or significant changes
        if (isActivelyPatrolling)
        {
            // Only log detailed patrol info every 2 seconds to reduce spam
            if (Time.frameCount % 120 == 0)
            {
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Currently patrolling - currentIndex: {currentPatrolIndex}, patrolPoints.Length: {(patrolPoints?.Length ?? 0)}");
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Current position: {transform.position}, Current destination: {currentDestination}");
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: isActivelyPatrolling: {isActivelyPatrolling}, MovementStatus: {currentMovementStatus}");
                
                if (patrolPoints != null && patrolPoints.Length > currentPatrolIndex)
                {
                    MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Target patrol point: {patrolPoints[currentPatrolIndex].position}");
                    MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Distance to target: {Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position):F2}");
                }
            }
        }
        
        // Check if destination reached for patrol logic
        if (isActivelyPatrolling && patrolPoints != null && patrolPoints.Length > 0 && HasReachedDestination())
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Reached patrol point {currentPatrolIndex}, moving to next");
            // Move to next patrol point
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            if (patrolPoints.Length > 0)
            {
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Moving to patrol point {currentPatrolIndex} at {patrolPoints[currentPatrolIndex].position}");
                MoveTo(patrolPoints[currentPatrolIndex].position, patrolSpeed);
            }
        }
        // Continue steering-based movement towards current destination
        else if (currentMovementStatus == MovementStatus.Moving && currentDestination != Vector3.zero)
        {
            // Use maxSpeed instead of currentMovementSpeed for more aggressive movement
            float targetSpeed = Mathf.Max(currentMovementSpeed, maxSpeed * 0.5f); // At least half max speed
            Vector3 steering = Steering.Seek(transform.position, currentDestination, _vel, targetSpeed);

            // Debug steering calculation
            if (Time.frameCount % 60 == 0)
            {
                MyLogger.LogInfo($"[STEERING CALC] Pos: {transform.position}, Target: {currentDestination}, Vel: {_vel}, TargetSpeed: {targetSpeed}");
                MyLogger.LogInfo($"[STEERING CALC] Calculated steering: {steering}");
            }

            ApplySteering(steering);
        }
        else if (currentMovementStatus == MovementStatus.Patrolling && patrolPoints != null && patrolPoints.Length > 0)
        {
            // Continue moving towards current patrol point
            if (currentPatrolIndex < patrolPoints.Length)
            {
                // Use Seek instead of Arrive for patrol movement to maintain constant speed
                Vector3 steering = Steering.Seek(transform.position, patrolPoints[currentPatrolIndex].position, _vel, patrolSpeed);
                ApplySteering(steering);
            }
        }
        
        // Check movement constraints
        if (hasMovementConstraints && !movementConstraints.Contains(transform.position))
        {
            MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: Movement constrained at position {transform.position}");
            currentMovementStatus = MovementStatus.Constrained;
            OnMovementBlocked?.Invoke();
        }
    }
    
    private void UpdateAISystem()
    {
        if (!HasValidTarget())
        {
            ClearTargetTransform();
        }

        bool hasPlayer = HasValidTarget();
        bool canSeePlayerNow = hasPlayer && CanSeePlayer();

        if (canSeePlayerNow)
        {
            lastKnownPlayerPosition = player.position;
            lastTimeSawPlayer = Time.time;
        }

        if (enableNewAISystem && m_blackboardService != null && hasPlayer)
        {
            // Update blackboard with current player information
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, player);
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, player.position);
            
            if (canSeePlayerNow)
            {
                m_blackboardService.SetValue(BlackboardKeys.LAST_KNOWN_PLAYER_POSITION, lastKnownPlayerPosition);
            }
            
            // Update detection information
            var detectionResult = GetDetectionResult();
            m_blackboardService.SetValue($"Guard_{gameObject.GetInstanceID()}_DetectionLevel", detectionResult.level);
            m_blackboardService.SetValue($"Guard_{gameObject.GetInstanceID()}_CanSeePlayer", detectionResult.level > PlayerDetectionLevel.None);
        }

        TryAssignDetainTargetFromPlayer(canSeePlayerNow);
    }

    private void HandleHealthRegen()
    {
        if (!enableHealthRegen || !isAlive) return;
        if (currentHealth >= MaxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        currentHealth = Mathf.Min(currentHealth + regenRate * Time.deltaTime, MaxHealth);
    }

    private void OnDisable()
    {
        UnsubscribeUpdateService();
    }

    private void OnEnable()
    {
        // When coming back from pool the listener may be unsubscribed; re-add it.
        SubscribeUpdateService();
    }

    private void SetupPatrolPoints()
    {
        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: SetupPatrolPoints called");
        
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: No patrol points assigned, creating default ones");
            patrolPoints = new Transform[2];
            
            GameObject point1 = new GameObject("PatrolPoint1");
            point1.transform.position = transform.position + Vector3.forward * 5f;
            patrolPoints[0] = point1.transform;
            
            GameObject point2 = new GameObject("PatrolPoint2");
            point2.transform.position = transform.position + Vector3.back * 5f;
            patrolPoints[1] = point2.transform;
            
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Created patrol points at {point1.transform.position} and {point2.transform.position}");
        }
        else
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Using {patrolPoints.Length} existing patrol points");
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Patrol point {i}: {patrolPoints[i].position}");
                }
                else
                {
                    MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: Patrol point {i} is null!");
                }
            }
        }
    }
    
    public void StartPatrol()
    {
        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: StartPatrol called");
        
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: Cannot start patrol - no patrol points assigned");
            return;
        }
        
        // Initialize patrol state
        currentPatrolIndex = 0;
        currentMovementStatus = MovementStatus.Patrolling;
        currentMovementMode = MovementMode.Walk;
        isActivelyPatrolling = true;  // Set patrol flag
        
        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Starting patrol with {patrolPoints.Length} points, moving to point 0");
        
        // Move to first patrol point
        MoveTo(patrolPoints[currentPatrolIndex].position, patrolSpeed);
        
        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Patrol started successfully");
    }
    
    #region Steering Physics

    /// <summary>
    /// Integrate steering force to update velocity with mass and force limits
    /// </summary>
    private Vector3 Integrate(Vector3 steering, float dt)
    {
        // Clamp steering force to maximum
        Vector3 clampedForce = steering;
        if (clampedForce.sqrMagnitude > maxForce * maxForce)
        {
            clampedForce = clampedForce.normalized * maxForce;
        }

        // Apply force to velocity (F = ma, so a = F/m)
        Vector3 acceleration = clampedForce / mass;
        Vector3 newVel = _vel + acceleration * dt;

        // Clamp velocity to maximum speed
        if (newVel.sqrMagnitude > maxSpeed * maxSpeed)
        {
            newVel = newVel.normalized * maxSpeed;
        }

        return newVel;
    }

    /// <summary>
    /// Apply steering force with optional flocking blend, obstacle avoidance and movement
    /// </summary>
    public void ApplySteering(Vector3 steering)
    {
        if (!isAlive) return;
        if (isMovementPaused) return;

        Vector3 finalSteering = steering;

        // Blend flocking forces if enabled and active
        if (useFlocking && flockingEntity != null && ShouldFlock())
        {
            Vector3 flockingForce = flockingEntity.GetFlockingForce();
            finalSteering = (steering * baseForceWeight) + (flockingForce * flockForceWeight);

            if (Time.frameCount % 60 == 0)
            {
                MyLogger.LogInfo($"[FLOCK BLEND] {gameObject.name}: base={steering.magnitude:F2}, flock={flockingForce.magnitude:F2}, final={finalSteering.magnitude:F2}");
            }
        }

        // 1) Integrate steering into desired velocity
        Vector3 desiredVel = Integrate(finalSteering, Time.deltaTime);
        desiredVel.y = 0f;

        float desiredSpeed = desiredVel.magnitude;
        if (desiredSpeed <= 0.0001f) return;

        Vector3 desiredDir = desiredVel / Mathf.Max(desiredSpeed, 1e-5f);

        // 2) Obstacle avoidance can now push opposite to desired direction
        Vector3 avoidedVel = obstacleAvoidance.GetDirImproved(desiredVel, false);
        Vector3 avoidanceDelta = avoidedVel - desiredVel;
        Vector3 avoidDir = avoidanceDelta.sqrMagnitude > 1e-6f ? avoidanceDelta.normalized : Vector3.zero;

        float pathW = 1.0f;
        float avoidW = 0.35f;

        if (avoidDir != Vector3.zero)
        {
            float oppositeFactor = Mathf.Clamp01(-Vector3.Dot(avoidDir, desiredDir));
            float weightBoost = Mathf.Lerp(0f, 0.75f, oppositeFactor);
            avoidW += weightBoost;

            Color debugColor = Color.Lerp(Color.yellow, Color.red, oppositeFactor);
            Debug.DrawRay(transform.position, avoidDir * 2f, debugColor, 0.1f);
        }
        avoidW = Mathf.Clamp(avoidW, 0f, 1f);

        float avoidScale = Mathf.Max(desiredSpeed, 0.1f);
        Vector3 blended = (desiredVel * pathW) + (avoidDir * (avoidW * avoidScale));

        // Clamp while prioritizing the path component
        float maxV = maxSpeed;
        if (blended.sqrMagnitude > maxV * maxV)
        {
            Vector3 pathComponent = Vector3.Project(blended, desiredDir);
            Vector3 avoidComponent = blended - pathComponent;

            float pathMag = pathComponent.magnitude;
            float avoidMag = avoidComponent.magnitude;
            float totalMag = Mathf.Sqrt(pathMag * pathMag + avoidMag * avoidMag);

            if (totalMag > maxV)
            {
                float scale = Mathf.Sqrt(Mathf.Max(0, maxV * maxV - pathMag * pathMag)) / Mathf.Max(avoidMag, 1e-5f);
                avoidComponent *= Mathf.Min(scale, 1f);
                blended = pathComponent + avoidComponent;
            }
        }

        blended.y = 0f;
        _vel = blended;

        // 3) Move and face movement direction
        if (_vel.sqrMagnitude > 0.001f)
        {
            float effectiveDeltaTime = Time.deltaTime;
            Vector3 movement = _vel * effectiveDeltaTime;
            transform.position += movement;

            currentMovementDirection = _vel.normalized;
            currentMovementSpeed = _vel.magnitude;

            if (_vel.magnitude > 0.1f)
            {
                Vector3 lookDirection = _vel.normalized;
                lookDirection.y = 0f;

                float rotationSpeed = baseRotationSpeed * 3f;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        if (Time.frameCount % 30 == 0)
        {
            float effectiveDeltaTime = Mathf.Max(Time.deltaTime, 0.016f);
            MyLogger.LogInfo($"[STEERING DEBUG] {gameObject.name}: Vel: {_vel.magnitude:F2}, AvoidDir: {avoidDir.magnitude:F2}, MaxSpeed: {maxSpeed}");
            MyLogger.LogInfo($"[STEERING DEBUG] Raw steering: {finalSteering.magnitude:F2}, DeltaTime: {Time.deltaTime:F4}, Movement: {(_vel * effectiveDeltaTime).magnitude:F4}");
        }
    }

    /// <summary>
    /// Determine if flocking should be active based on current guard state
    /// </summary>
    public bool ShouldFlock()
    {
        if (!useFlocking || !isAlive)
            return false;

        if (useFSM && stateMachine != null)
        {
            var stateName = stateMachine.GetCurrentState()?.State?.StateName;
            // Enable flocking during patrol and chase for group coordination
            return stateName == "S_GuardPatrol" || stateName == "S_GuardChase";
        }

        // Fallback heuristic: flock while patrolling or when seeing player (group chase)
        return isActivelyPatrolling || CanSeePlayer();
    }

    public void PauseMovement(bool pause)
    {
        isMovementPaused = pause;
        if (pause)
        {
            _vel = Vector3.zero;
            currentMovementStatus = MovementStatus.Idle;
        }
    }
    
    #endregion

    public override void Move(Vector3 direction)
    {
        if (!isAlive)
        {
            // Only log this occasionally if it's being called repeatedly
            if (Time.frameCount % 120 == 0)
            {
                MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: Move called but not alive");
            }
            return;
        }

        // Legacy direct movement - use maxSpeed instead of characterData.moveSpeed
        Vector3 targetVel = direction.normalized * maxSpeed;
        Vector3 steering = targetVel - _vel;
        ApplySteering(steering);
    }
    
    public override void Shoot(Vector3 direction)
    {
        if (!HasValidTarget()) return;
        if (!isAlive || !CanShoot()) return;
        
        lastShootTime = Time.time;
        CreateBullet(direction);
        
        // MEJORA: Update blackboard with combat information
        if (enableNewAISystem && m_blackboardService != null)
        {
            m_blackboardService.SetValue($"Guard_{gameObject.GetInstanceID()}_LastShootTime", lastShootTime);
            m_blackboardService.SetValue($"Guard_{gameObject.GetInstanceID()}_ShootDirection", direction);
        }
    }

    private void CreateBullet(Vector3 p_direction)
    {
        var l_spawnPosition = transform.position + Vector3.up * 0.5f + p_direction * 0.8f;
        var l_bullet = PoolObjectsService.GetOrCreateObject(bulletData.Prefab);
        l_bullet.OnDeactivate += OnDeactivateBulletHandler;
        l_bullet.InitializeBullet(bulletData, l_spawnPosition, p_direction, BulletOwner.Guard, gameObject.name);
    }

    private void OnDeactivateBulletHandler(BulletObject p_bullet)
    {
        p_bullet.OnDeactivate -= OnDeactivateBulletHandler;
        PoolObjectsService.ReturnObject(p_bullet);
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        lastDamageTime = Time.time;
    }

    private void TryAssignDetainTargetFromPlayer(bool canSeePlayerNow)
    {
        if (isDetaining || detainTarget != null || player == null || !isAlive)
            return;

        if (!canSeePlayerNow)
            return;

        Vector3 toGuard = (transform.position - player.position);
        toGuard.y = 0f;
        if (player.forward.sqrMagnitude < 0.001f)
            return;

        float angle = Vector3.Angle(player.forward, toGuard.normalized);
        // We only detain if we are behind the player: angle should be close to 180
        if (angle < 180f - detainBackAngle * 0.5f)
            return;

        SetDetainTarget(player);
    }

    #region AI System Integration
    
    public Transform GetModelTransform()
    {
        return transform;
    }
    
    public void SetTargetTransform(Transform p_target)
    {
        player = p_target;
        targetCharacter = p_target != null ? p_target.GetComponentInParent<BaseCharacter>() : null;

        if (!HasValidTarget())
        {
            ClearTargetTransform();
            return;
        }
        
        // Update AI system when target changes
        if (enableNewAISystem && m_blackboardService != null && player != null)
        {
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, player);
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, player.position);
        }
    }
    
    public Transform GetTargetTransform()
    {
        return player;
    }

    private bool HasValidTarget()
    {
        if (player != null && targetCharacter == null)
        {
            targetCharacter = player.GetComponentInParent<BaseCharacter>();
        }

        if (player == null) return false;
        if (!player.gameObject.activeInHierarchy) return false;
        if (targetCharacter != null && !targetCharacter.IsAlive) return false;
        return true;
    }

    private void ClearTargetTransform()
    {
        player = null;
        targetCharacter = null;

        if (enableNewAISystem && m_blackboardService != null)
        {
            m_blackboardService.SetValue<Transform>(BlackboardKeys.PLAYER_TRANSFORM, null);
            m_blackboardService.SetValue<Vector3>(BlackboardKeys.PLAYER_POSITION, Vector3.zero);
        }
    }
    
    #endregion
    
    #region IAIMovementController Implementation
    
    public bool CanMove()
    {
        return isAlive && IsActive;
    }
    
    // MEJORA: Extended IAIMovementController methods
    public void MoveTo(Vector3 target, float speed)
    {
        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: MoveTo called - Target: {target}, Speed: {speed}, CanMove: {CanMove()}");

        if (!CanMove())
        {
            MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: MoveTo blocked - CanMove returned false");
            return;
        }

        currentDestination = target;
        currentMovementSpeed = speed;
        currentMovementStatus = MovementStatus.Moving;

        // Check constraints
        if (hasMovementConstraints && !movementConstraints.Contains(target))
        {
            MyLogger.LogWarning($"[PATROL DEBUG] {gameObject.name}: MoveTo constrained - target outside bounds");
            currentMovementStatus = MovementStatus.Constrained;
            OnMovementBlocked?.Invoke();
            return;
        }

        // For patrol movement, use Seek to maintain constant speed
        // For precise positioning (like reaching a specific point), use Arrive
        float distanceToTarget = Vector3.Distance(transform.position, target);
        Vector3 steering;

        if (distanceToTarget > slowingDistance * 2f)
        {
            // Use Seek for constant speed when far from target
            steering = Steering.Seek(transform.position, target, _vel, speed);
        }
        else
        {
            // Use Arrive for smooth stop near target
            steering = Steering.Arrive(transform.position, target, _vel, speed, slowingDistance);
        }

        ApplySteering(steering);

        MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: MoveTo using steering - Destination: {currentDestination}, Status: {currentMovementStatus}");
    }
    
    public bool HasReachedDestination()
    {
        if (currentMovementStatus == MovementStatus.Idle) 
        {
            // Only log this occasionally as it might be checked frequently
            if (Time.frameCount % 120 == 0)
            {
                MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: HasReachedDestination - Status is Idle, returning true");
            }
            return true;
        }
        
        float distanceToDestination = Vector3.Distance(transform.position, currentDestination);
        bool reached = distanceToDestination < 0.5f;
        
        // Only log distance checks when close to destination or occasionally
        if (reached || Time.frameCount % 120 == 0)
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: HasReachedDestination - Distance: {distanceToDestination:F2}, Reached: {reached}, Status: {currentMovementStatus}");
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Current pos: {transform.position}, Destination: {currentDestination}");
        }
        
        if (reached && currentMovementStatus == MovementStatus.Moving)
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: Destination reached, changing status to Idle");
            currentMovementStatus = MovementStatus.Idle;
            OnMovementComplete?.Invoke();
        }
        
        return reached;
    }
    
    public void FaceDirection(Vector3 direction, float rotationSpeed = -1f)
    {
        if (!CanMove()) return;
        
        if (direction.magnitude > 0.1f)
        {
            float speed = rotationSpeed > 0 ? rotationSpeed : baseRotationSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }
    
    #endregion
    
    #region MEJORA: Advanced AI Methods
    
    /// <summary>
    /// Get threat assessment based on current detection and situation
    /// </summary>
    public float GetThreatLevel()
    {
        if (!HasValidTarget()) return 0f;

        if (aiContext != null)
        {
            return aiContext.GetThreatLevel();
        }
        
        // Fallback calculation
        if (!CanSeePlayer()) return 0f;
        
        float distance = Vector3.Distance(transform.position, player.position);
        float distanceFactor = 1f - Mathf.Clamp01(distance / detectionRange);
        
        return distanceFactor * 0.7f; // Base threat level
    }
    
    /// <summary>
    /// Get confidence in current player information
    /// </summary>
    public float GetInformationConfidence()
    {
        if (aiContext != null)
        {
            return aiContext.GetInformationConfidence();
        }
        
        // Fallback: if we can see player, confidence is high
        return CanSeePlayer() ? 1.0f : 0.2f;
    }
    
    /// <summary>
    /// Should the guard investigate based on detection level and personality
    /// </summary>
    public bool ShouldInvestigate()
    {
        var detectionResult = GetDetectionResult();
        
        return personalityType switch
        {
            AIPersonalityType.Aggressive => detectionResult.level >= PlayerDetectionLevel.Peripheral,
            AIPersonalityType.Cautious => detectionResult.level >= PlayerDetectionLevel.Partial,
            AIPersonalityType.Conservative => detectionResult.level >= PlayerDetectionLevel.Clear,
            _ => detectionResult.level >= PlayerDetectionLevel.Partial
        };
    }
    
    /// <summary>
    /// Should the guard enter combat based on detection and personality
    /// </summary>
    public bool ShouldAttack()
    {
        if (!HasValidTarget()) return false;

        var detectionResult = GetDetectionResult();
        float distance = Vector3.Distance(transform.position, player.position);

        bool inAttackRange = distance <= attackRange;
        bool canSee = detectionResult.level >= PlayerDetectionLevel.Clear;

        return personalityType switch
        {
            AIPersonalityType.Aggressive => canSee && distance <= detectionRange,
            AIPersonalityType.Cautious => canSee && inAttackRange,
            AIPersonalityType.Conservative => canSee && inAttackRange && GetThreatLevel() > 0.7f,
            _ => canSee && inAttackRange
        };
    }

    /// <summary>
    /// Pursue the player using steering behaviors
    /// </summary>
    public void PursuePlayer()
    {
        if (!HasValidTarget()) return;

        Vector3 playerVel = Vector3.zero;
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerVel = playerRb.linearVelocity;
        }

        Vector3 steering = Steering.Pursuit(transform.position, _vel, player.position, playerVel, chaseSpeed);
        ApplySteering(steering);

        currentMovementStatus = MovementStatus.Moving;
        currentDestination = player.position;
    }

    /// <summary>
    /// Evade from the player using steering behaviors
    /// </summary>
    public void EvadePlayer()
    {
        if (!HasValidTarget()) return;

        Vector3 playerVel = Vector3.zero;
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerVel = playerRb.linearVelocity;
        }

        Vector3 steering = Steering.Evade(transform.position, _vel, player.position, playerVel, chaseSpeed);
        ApplySteering(steering);

        currentMovementStatus = MovementStatus.Fleeing;
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        currentDestination = transform.position + fleeDirection * 10f;
    }
    
    /// <summary>
    /// Get movement speed based on current state and personality
    /// </summary>
    public float GetContextualSpeed()
    {
        var detectionResult = GetDetectionResult();

        float baseSpeed = detectionResult.level switch
        {
            PlayerDetectionLevel.None => patrolSpeed,
            PlayerDetectionLevel.Peripheral => patrolSpeed * 1.2f,
            PlayerDetectionLevel.Partial => personalityType == AIPersonalityType.Aggressive ? chaseSpeed * 0.8f : patrolSpeed * 1.5f,
            PlayerDetectionLevel.Clear => chaseSpeed,
            PlayerDetectionLevel.Immediate => chaseSpeed * 1.2f,
            _ => patrolSpeed
        };

        // Ensure we don't exceed maxSpeed
        return Mathf.Min(baseSpeed, maxSpeed);
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Print AI Status")]
    public void PrintAIStatus()
    {
        MyLogger.LogInfo("=== GUARD AI STATUS ===");
        MyLogger.LogInfo($"AI System Enabled: {enableNewAISystem}");
        MyLogger.LogInfo($"Personality: {personalityType}");
        MyLogger.LogInfo($"Can See Player: {CanSeePlayer()}");

        var detectionResult = GetDetectionResult();
        MyLogger.LogInfo($"Detection Level: {detectionResult.level}");
        MyLogger.LogInfo($"Threat Level: {GetThreatLevel():F2}");
        MyLogger.LogInfo($"Information Confidence: {GetInformationConfidence():F2}");
        MyLogger.LogInfo($"Should Investigate: {ShouldInvestigate()}");
        MyLogger.LogInfo($"Should Attack: {ShouldAttack()}");
        MyLogger.LogInfo($"Contextual Speed: {GetContextualSpeed():F1}");

        // Steering physics status
        MyLogger.LogInfo($"Current Velocity: {_vel} (magnitude: {_vel.magnitude:F2})");
        MyLogger.LogInfo($"Max Speed: {maxSpeed}, Max Force: {maxForce}, Mass: {mass}");

        if (player != null)
        {
            MyLogger.LogInfo($"Distance to Player: {Vector3.Distance(transform.position, player.position):F2}");
        }
        MyLogger.LogInfo("======================");
    }

    [ContextMenu("Test Pursue Player")]
    private void TestPursuePlayer()
    {
        if (player != null)
        {
            PursuePlayer();
            MyLogger.LogInfo("Started pursuing player using steering behaviors");
        }
        else
        {
            MyLogger.LogInfo("No player found to pursue");
        }
    }

    [ContextMenu("Test Evade Player")]
    private void TestEvadePlayer()
    {
        if (player != null)
        {
            EvadePlayer();
            MyLogger.LogInfo("Started evading player using steering behaviors");
        }
        else
        {
            MyLogger.LogInfo("No player found to evade from");
        }
    }

    [ContextMenu("Test Direct Movement")]
    private void TestDirectMovement()
    {
        Vector3 testDirection = transform.forward;
        Move(testDirection);
        MyLogger.LogInfo($"Applied direct movement - Direction: {testDirection}, Current Vel: {_vel.magnitude:F2}");
    }

    [ContextMenu("Reset Velocity")]
    private void ResetVelocity()
    {
        _vel = Vector3.zero;
        MyLogger.LogInfo("Velocity reset to zero");
    }

    [ContextMenu("Force High Speed")]
    private void ForceHighSpeed()
    {
        mass = 0.1f;
        maxForce = 100f;
        maxSpeed = 20f;
        slowingDistance = 0.5f;
        MyLogger.LogInfo($"Forced high speed settings: Mass={mass}, MaxForce={maxForce}, MaxSpeed={maxSpeed}");
    }

    [ContextMenu("Test Seek Behavior")]
    private void TestSeekBehavior()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Vector3 target = patrolPoints[0].position;
            MyLogger.LogInfo($"=== SEEK TEST ===");
            MyLogger.LogInfo($"Position: {transform.position}");
            MyLogger.LogInfo($"Target: {target}");
            MyLogger.LogInfo($"Current Vel: {_vel}");
            MyLogger.LogInfo($"Max Speed: {maxSpeed}");

            Vector3 steering = Steering.Seek(transform.position, target, _vel, maxSpeed);
            MyLogger.LogInfo($"Calculated steering: {steering}, magnitude: {steering.magnitude:F2}");

            // Calculate expected values manually
            Vector3 desired = target - transform.position;
            desired.y = 0f;
            desired = desired.normalized * maxSpeed;
            Vector3 expectedSteering = desired - _vel;
            MyLogger.LogInfo($"Expected desired: {desired}");
            MyLogger.LogInfo($"Expected steering: {expectedSteering}");

            ApplySteering(steering);
        }
    }

    [ContextMenu("Force Manual Movement")]
    private void ForceManualMovement()
    {
        Vector3 forceVel = transform.forward * 5f;
        _vel = forceVel;
        transform.position += _vel * Time.deltaTime;
        MyLogger.LogInfo($"Forced velocity: {_vel}, moved to: {transform.position}");
    }

    [ContextMenu("Debug Complete Steering Pipeline")]
    private void DebugSteeringPipeline()
    {
        MyLogger.LogInfo("=== COMPLETE STEERING DEBUG ===");
        MyLogger.LogInfo($"Current Status: isAlive={isAlive}, currentMovementStatus={currentMovementStatus}");
        MyLogger.LogInfo($"Current destination: {currentDestination}");
        MyLogger.LogInfo($"Physics: mass={mass}, maxForce={maxForce}, maxSpeed={maxSpeed}");
        MyLogger.LogInfo($"Current velocity: {_vel}");
        MyLogger.LogInfo($"Time.deltaTime: {Time.deltaTime:F6}, FPS: {1f/Time.deltaTime:F1}");

        if (currentDestination != Vector3.zero)
        {
            // Test direct steering calculation
            Vector3 steering = Steering.Seek(transform.position, currentDestination, _vel, maxSpeed);
            MyLogger.LogInfo($"Direct Seek result: {steering}");

            // Test integration
            Vector3 integratedVel = Integrate(steering, Time.deltaTime);
            MyLogger.LogInfo($"After integration: {integratedVel}");

            // Test obstacle avoidance
            Vector3 avoidedVel = obstacleAvoidance.GetDirImproved(integratedVel, false);
            MyLogger.LogInfo($"After obstacle avoidance: {avoidedVel}");

            // Calculate final movement
            float effectiveDeltaTime = Mathf.Max(Time.deltaTime, 0.016f);
            Vector3 finalMovement = avoidedVel * effectiveDeltaTime;
            MyLogger.LogInfo($"Final movement per frame: {finalMovement.magnitude:F6} units");
            MyLogger.LogInfo($"Movement per second: {finalMovement.magnitude * (1f/effectiveDeltaTime):F2} units/sec");

            // Apply directly
            ApplySteering(steering);
        }
    }

    [ContextMenu("Test High Speed Movement")]
    private void TestHighSpeedMovement()
    {
        // Bypass all steering and move directly at high speed
        Vector3 direction = (currentDestination - transform.position).normalized;
        Vector3 highSpeedMovement = direction * 2f; // 2 units per frame = 120 units/sec at 60fps
        transform.position += highSpeedMovement;
        MyLogger.LogInfo($"Direct high speed movement: {highSpeedMovement.magnitude} units per frame");
    }

    [ContextMenu("Debug FSM Status")]
    private void DebugFSMStatus()
    {
        MyLogger.LogInfo("=== FSM STATUS ===");
        MyLogger.LogInfo($"Use FSM: {useFSM}");
        MyLogger.LogInfo($"State Data Count: {stateDataList?.Count ?? 0}");
        MyLogger.LogInfo($"StateMachine Initialized: {stateMachine != null}");

        if (stateMachine != null)
        {
            var currentState = stateMachine.GetCurrentState();
            MyLogger.LogInfo($"Current State: {currentState?.State?.StateName ?? "None"}");
        }

        MyLogger.LogInfo($"Current Patrol Loops: {CurrentPatrolLoops}/{LoopsToIdle}");
        MyLogger.LogInfo($"Patrol Direction: {(PatrolDirection ? "Forward" : "Backward")}");
        MyLogger.LogInfo($"Current Patrol Index: {CurrentPatrolIndex}");
        MyLogger.LogInfo($"Has Reached Current Point: {HasReachedCurrentPatrolPoint}");
        MyLogger.LogInfo($"State Timer: {StateTimer:F2}");
        MyLogger.LogInfo("==================");
    }

    #endregion

    protected override void OnDeath()
    {
        base.OnDeath();

        if (UGS_Analytics.Instance != null)
        {
            UGS_Analytics.Instance.LogGuardKilled(gameObject.name, transform.position);
        }
    }

    #region IUseFsm Implementation

    public void UpdateFsm()
    {
        // This method exists for interface compatibility but we call RunStateMachine directly in OnUpdate
        stateMachine?.RunStateMachine();
    }

    // GetModelTransform, SetTargetTransform, and GetTargetTransform are already implemented above

    #endregion

    #region Configuration Methods for Subclasses and External Systems

    /// <summary>
    /// Set patrol points for this guard. Public because it's used by external spawners.
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
    }

    #endregion

    private bool HandleLeaderOverride()
    {
        if (!leaderOverrideActive)
            return false;

        if (ignoreLeaderDuringDetain && (isDetaining || isKnockedOut))
            return false;

        // Si vemos al jugador, liberamos la orden para volver a la FSM de combate
        if (CanSeePlayer())
        {
            ClearLeaderOverride();
            return false;
        }

        if (Time.time >= leaderOverrideExpiresAt)
        {
            ClearLeaderOverride();
            return false;
        }

        // Move toward override target unless reached and dwell time satisfied
        float distance = Vector3.Distance(transform.position, leaderOverrideTarget);
        if (distance > overrideArrivalTolerance)
        {
            // Mantiene viva la orden mientras se esté desplazando
            leaderOverrideExpiresAt = Time.time + overrideDuration;

            Vector3 steering = Game.AI.Steering.Steering.Arrive(
                transform.position,
                leaderOverrideTarget,
                _vel,
                patrolSpeed,
                slowingDistance);

            ApplySteering(steering);

            currentMovementStatus = MovementStatus.Moving;
        }
        else
        {
            if (leaderOverrideReachedAt < 0f)
            {
                leaderOverrideReachedAt = Time.time;
            }
            else if (Time.time - leaderOverrideReachedAt >= overrideDwellTime)
            {
                ClearLeaderOverride();
                return false;
            }
            else
            {
                ApplySteering(Vector3.zero);
                currentMovementStatus = MovementStatus.Idle;
            }
        }

        return leaderOverrideActive;
    }

    public void MyUpdate()
    {
        if (!isAlive) return;

        if (!HasValidTarget())
        {
            ClearTargetTransform();
        }

        // Add debug log with reduced frequency to avoid spam
        if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second at 60fps)
        {
            MyLogger.LogInfo($"[PATROL DEBUG] {gameObject.name}: OnUpdate is being called - Frame {Time.frameCount}");
        }

        UpdateAISystem();

        if (HandleLeaderOverride())
            return;

        // Run FSM if enabled, otherwise use legacy movement system
        if (useFSM && stateMachine != null)
        {
            stateMachine.RunStateMachine();
            // In FSM mode, states handle their own timers - don't auto-increment
        }
        else
        {
            UpdateMovementSystem(Time.deltaTime);
            stateTimer += Time.deltaTime; // Keep timer for conditions in legacy mode
        }

        HandleHealthRegen();
    }

    public void SubscribeUpdateService()
    {
        ServiceLocator.Get<IUpdateService>().AddUpdateListener(this);
    }

    public void UnsubscribeUpdateService()
    {
        ServiceLocator.Get<IUpdateService>().RemoveUpdateListener(this);
    }

    public virtual void ResetFromPool()
    {
        ClearLeaderOverride(null, true);
        ClearCoverPoint();
        steeringTarget = null;
        currentMovementStatus = MovementStatus.Idle;
        isMovementPaused = false;
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!showStateLabel) return;

        FsmGizmoHelper.DrawStateLabel(transform, $"State: {CurrentStateName}", Color.cyan, 2f);

        // Cover point debug
        if (HasCoverPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(CoverPoint, 0.2f);
            Gizmos.DrawLine(transform.position, CoverPoint);
        }

        // Obstacle collider debug
        if (LastCoverCollider != null)
        {
            var bounds = LastCoverCollider.bounds;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if (LastCoverHitPoint != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(LastCoverHitPoint, 0.1f);
                if (LastCoverHitNormal != Vector3.zero)
                {
                    Gizmos.DrawRay(LastCoverHitPoint, LastCoverHitNormal * 0.75f);
                }
            }
        }
    }
#endif
}
