using UnityEngine;
using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using System.Collections.Generic;
using System.Linq;
using Services.MicroServices.AudioService;
using Services.MicroServices.BlackboardService;
using Services;
using Services.MicroServices.UpdateService;
using Unity.Assertions;
using UnityEditor;

public class Civilian : BaseCharacter, IUseFsm, IUpdateListener
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 1.5f;        // Idle drift speed
    [SerializeField] private float fleeSpeed = 7f;          // Must be > playerSpeed (5f)
    [SerializeField] private float evadeSpeed = 8f;         // Slightly higher than flee
    [SerializeField] private float pursueSpeed = 3f;        // <= guard chase (4f)

    [Header("Detection Settings")]
    [SerializeField] private float sightFOV = 120f;         // Field of view angle
    [SerializeField] private float sightRange = 6f;         // Detection range
    [SerializeField] private float meleeRange = 1.5f;       // Close contact range

    [Header("Behavior Distances & Timers")]
    [SerializeField] private float safeDistance = 10f;      // Distance to stop fleeing
    [SerializeField] private float idleSecondsAfterSafe = 3f; // Idle time after reaching safety
    [SerializeField] private float loseSightGrace = 2f;     // Grace period after losing sight

    [Header("Dying Behavior")]
    [SerializeField] private float dyingWeightMax = 1.5f;
    [SerializeField] private AnimationCurve dyingWeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 1f)] private float dyingSpeedMultiplier = 0.35f;

    [Header("Attack Configuration")]
    [SerializeField] private float attackWindup = 0.35f;    // Seconds before hit
    [SerializeField] private float attackHitWin = 0.10f;    // Hit window duration
    [SerializeField] private float attackRecover = 0.35f;   // Recovery after hit
    [SerializeField] private float attackLoseSightGrace = 0.3f; // Time before aborting attack
    [SerializeField] private int meleeDamage = 1;           // Damage per melee hit
    [SerializeField] private Color attackColor = Color.red; // Visual feedback while attacking

    [Header("Steering Physics")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float maxForce = 15f;
    [SerializeField] private float slowingDistance = 2f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstaclesMask = -1;
    [SerializeField] private float avoidRadius = 1.5f;
    [SerializeField] private float avoidAngle = 90f;
    [SerializeField] private float personalArea = 0.3f;

    [Header("FSM Configuration")]
    [SerializeField] private float evadeTime = 1f;          // Duration of evade state (0.75-1.25s)
    [SerializeField] private float safeTime = 2f;           // Time to maintain safety before idle
    [SerializeField] private List<StateData> stateDataList = new List<StateData>();
    [SerializeField] private bool useFSM = true;

    [Header("Decision Tree")]
    [SerializeField] private bool useDecisionTree = true;   // Enable/disable decision tree system

    [Header("Civilian Info")]
    [SerializeField] private NPCGender gender = NPCGender.Male;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool canAttack = false;        // Civilians typically don't attack

    // --- Pathfinding (mobile-friendly) ---
    [Header("Pathfinding")]
    [SerializeField] private GraphAsset fleeGraph;
    [SerializeField] private int fleeTargetNodeIndex = -1;
    [SerializeField] private float fleeWaypointReach = 0.8f;//0.5f;
    [SerializeField] private float fleeRecomputeInterval = 1.0f;

    // --- Path Smoothing ---
    [Header("Path Smoothing")]
    [SerializeField] private bool usePathSmoothing = true;
    [Tooltip("Simple: Checks every N nodes. Full: Best quality.")]
    [SerializeField] private PathSmoothingMode smoothingMode = PathSmoothingMode.Full;
    [SerializeField] private int smoothingSkipStride = 2; // Check every 2nd node in simple mode

    public enum PathSmoothingMode { None, Simple, Full }

    // Reusable Buffers for Pathfinding (init in Start)
    private float[] _astarG, _astarF;
    private int[] _astarFrom, _heapIdx, _pathIdx;
    private float[] _heapF;
    private bool[] _astarClosed;

    // Path Buffers
    private Vector3[] _worldPath;    // Raw A* path
    private Vector3[] _smoothedPath; // Optimized path
    private PathFollowerAgent _pathFollower;
    private float _lastAStarTime = -999f;
    private int _pathLen = 0;
    private int _smoothedPathLen = 0;
    private int _graphCachedNodeCount = -1;
    private float dyingRouletteWeight = 0f;
    private bool isInDyingEvade = false;


    // Component references
    private Transform player;
    private IPlayerDetector playerDetector;
    private IBlackboardService m_blackboardService;
    private Renderer meshRenderer;
    private Material originalMaterial;
    private Color originalColor;
    private CivilianDecisionTreeRunner decisionTreeRunner;
    private AudioService m_audioService;
    private AudioConfig m_audioConfig;

    // Steering components (identical to Guard)
    private Vector3 _vel;
    private ObstacleAvoidance obstacleAvoidance;
    private float currentMaxSpeed;

    // FSM components
    private StateMachine stateMachine;
    private float stateTimer;
    private float safeTimer; // Timer for tracking safety duration
    private float pursuitLoseSightTimer; // Timer for tracking lose sight during pursuit

    // Legacy state tracking (for compatibility)
    private CivilianState currentState = CivilianState.Idle;
    private Vector3 lastKnownPlayerPosition;
    private bool hasEverSeenPlayer = false;

    // Movement state
    private Vector3 currentMovementDirection;
    private float currentMovementSpeed;

    public enum CivilianState
    {
        Idle,
        Fleeing,
        Evading,
        Safe
    }

    #region Properties

    public float WalkSpeed => walkSpeed;
    public float FleeSpeed => fleeSpeed;
    public float EvadeSpeed => evadeSpeed;
    public float PursueSpeed => pursueSpeed;
    public float SightFOV => sightFOV;
    public float SightRange => sightRange;
    public float MeleeRange => meleeRange;
    public float SafeDistance => safeDistance;
    public float IdleSecondsAfterSafe => idleSecondsAfterSafe;
    public float EvadeTime => evadeTime;
    public float SafeTime => safeTime;
    public float EscapeWeight => 1f; // Dinámico: se ajusta en runtime en el DecisionTreeRunner
    public float AttackWeight => 1f; // Dinámico: se ajusta en runtime en el DecisionTreeRunner
    public float DyingWeight => dyingRouletteWeight;
    public float DyingSpeed => Mathf.Max(0.1f, fleeSpeed * dyingSpeedMultiplier);
    public float HealthNormalized => Mathf.Clamp01(currentHealth / Mathf.Max(0.0001f, MaxHealth));
    public float HealthLostNormalized => 1f - HealthNormalized;
    public bool IsInDyingEvade => isInDyingEvade;
    public float AttackWindup => attackWindup;
    public float AttackHitWin => attackHitWin;
    public float AttackRecover => attackRecover;
    public float AttackLoseSightGrace => attackLoseSightGrace;
    public int MeleeDamage => meleeDamage;
    public Color AttackColor => attackColor;
    public bool CanAttack => canAttack;
    public bool EnableDebugLogs => enableDebugLogs;
    public bool UseDecisionTree => useDecisionTree;
    public Transform Player => player;
    public CivilianState CurrentState => currentState;
    public Vector3 CurrentVelocity => _vel;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    
    // FSM Properties
    public float StateTimer 
    { 
        get => stateTimer; 
        set => stateTimer = value; 
    }
    
    public float SafeTimer 
    { 
        get => safeTimer; 
        set => safeTimer = value; 
    }

    public float PursuitLoseSightTimer 
    { 
        get => pursuitLoseSightTimer; 
        set => pursuitLoseSightTimer = value; 
    }

    public void SetDyingEvadeMode(bool active)
    {
        isInDyingEvade = active;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        base.Awake();
        RecalculateDyingWeight();
        InitializeComponents();
        SubscribeUpdateService();
        //TryInitFleePathfinding();
    }

    private void Start()
    {
        TryInitFleePathfinding();
        InitializeSteering();
        FindPlayer();
        InitializeScriptableObjectFSM();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        // Audio now lives as a singleton MonoBehaviour instead of a registered service
        m_audioService = AudioService.Instance;
        m_audioConfig = m_audioService?.GetConfig();

        // Get or add PlayerDetector
        playerDetector = GetComponent<IPlayerDetector>();
        Assert.IsNotNull(playerDetector);

        // Get renderer for color changes during attacks
        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            originalMaterial = meshRenderer.material;
            originalColor = meshRenderer.material.color;
        }

        // Get blackboard service (read-only access)
        m_blackboardService = ServiceLocator.Get<IBlackboardService>();
        if (m_blackboardService == null && enableDebugLogs)
        {
            MyLogger.LogWarning($"Civilian {gameObject.name}: Blackboard service not available");
        }

        // Initialize decision tree runner if enabled
        if (useDecisionTree)
        {
            decisionTreeRunner = GetComponent<CivilianDecisionTreeRunner>();
            Assert.IsNotNull(decisionTreeRunner);
        }
    }

    private void InitializeSteering()
    {
        // Initialize steering physics (identical to Guard)
        _vel = Vector3.zero;
        obstacleAvoidance = new ObstacleAvoidance(transform, avoidRadius, avoidAngle, personalArea, obstaclesMask);
        currentMaxSpeed = walkSpeed;

        if (enableDebugLogs)
            MyLogger.LogInfo($"Civilian {gameObject.name}: Steering initialized");
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
                if (enableDebugLogs)
                    MyLogger.LogInfo($"Civilian {gameObject.name}: Found player at {player.name}");
            }
        }
    }

    private void InitializeFSM()
    {
        // Legacy FSM fallback - Start in Idle state
        currentState = CivilianState.Idle;
        
        if (enableDebugLogs)
            MyLogger.LogInfo($"Civilian {gameObject.name}: Legacy FSM initialized");
    }

    private void InitializeScriptableObjectFSM()
    {
        if (useFSM && stateDataList != null && stateDataList.Count > 0)
        {
            stateMachine = new StateMachine(stateDataList, this);
            
            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: ScriptableObject FSM initialized with {stateDataList.Count} states");
        }
        else
        {
            // Fallback to legacy FSM
            InitializeFSM();
            
            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: Using legacy FSM (ScriptableObject FSM disabled or no states configured)");
        }
    }

    //Pathfinding
    private void TryInitFleePathfinding()
    {
        // 1. AUTO-ASIGNACION: Si no tengo grafo, busco al Manager de la escena
        if (fleeGraph == null)
        {
            // Intento A: Usar el Singleton (mas rapido, sin busqueda)
            if (SceneGraphManager.Instance != null)
            {
                fleeGraph = SceneGraphManager.Instance.currentLevelGraph;
            }
            // Intento B: Buscar en la escena (version moderna y optimizada)
            else
            {
                // CAMBIO AQUI: Usamos FindAnyObjectByType en lugar de FindObjectOfType
                var manager = FindAnyObjectByType<SceneGraphManager>();
                if (manager != null)
                {
                    fleeGraph = manager.currentLevelGraph;
                }
            }
        }

        // 2. VALIDACION
        if (fleeGraph == null || fleeGraph.NodeCount <= 0)
        {
            if (enableDebugLogs) Debug.LogWarning($"[{name}] No FleeGraph found directly or via SceneGraphManager.");
            return;
        }

        // 3. INICIALIZACION DE BUFFERS
        if (_astarG != null && _graphCachedNodeCount == fleeGraph.NodeCount) return;

        AllocateFleeBuffers(fleeGraph.NodeCount);
    }

    private void AllocateFleeBuffers(int n)
    {
        _astarG = new float[n];
        _astarF = new float[n];
        _astarFrom = new int[n];
        _heapIdx = new int[n];
        _heapF = new float[n];
        _astarClosed = new bool[n];
        _pathIdx = new int[n];
        // World paths
        _worldPath = new Vector3[n];
        _smoothedPath = new Vector3[n]; // Buffer para path optimizado

        if (_pathFollower == null)
            _pathFollower = new PathFollowerAgent();

        _pathFollower.waypointReachDist = fleeWaypointReach;
        _pathFollower.slowingDistance = 1.0f;

        _graphCachedNodeCount = n;
        _pathLen = 0; // limpiar ruta previa si cambio el grafo
        _smoothedPathLen = 0;
    }

    private void ClearFleeBuffers()
    {
        _astarG = _astarF = null;
        _astarFrom = _heapIdx = _pathIdx = null;
        _heapF = null;
        _astarClosed = null;
        _worldPath = null;
        _smoothedPath = null;
        _graphCachedNodeCount = -1;
        _pathLen = 0;
        _smoothedPathLen = 0;
    }

    // este es el que invoca el estado: idempotente, rapido
    public void EnsureFleePathfindingInitialized()
    {
        if (fleeGraph == null || fleeGraph.NodeCount <= 0)
        {
            ClearFleeBuffers();
            return;
        }
        if (_astarG == null || _graphCachedNodeCount != fleeGraph.NodeCount)
            AllocateFleeBuffers(fleeGraph.NodeCount);
    }

    #endregion

    #region Pathfinding

    // 1) Closest node
    private int ClosestNodeIndex(Vector3 pos)
    {
        if (fleeGraph == null) return -1;
        int best = -1; float bestSq = float.PositiveInfinity;
        for (int i = 0; i < fleeGraph.NodeCount; i++)
        {
            float d = (fleeGraph.nodePositions[i] - pos).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = i; }
        }
        return best;
    }


    // 2) Syntactic sugar
    public bool HasFleeGraph =>
        fleeGraph != null && fleeGraph.NodeCount > 0 && fleeTargetNodeIndex >= 0 && fleeTargetNodeIndex < fleeGraph.NodeCount;

    //public bool HasFleePath => _pathLen > 0;
    // Verifica si tenemos un path SUAVIZADO valido
    public bool HasFleePath => _smoothedPathLen > 0;

    // 3) Recompute si hace falta (intervalo o path vacio)
    public void RecomputeFleePathIfNeeded(float now)
    {
        if (!HasFleeGraph)
        {
            _pathLen = 0;
            _smoothedPathLen = 0;
            return;
        }

        // ==== ESTRATEGIA: Solo recomputar si REALMENTE es necesario ====

        // 1) Si tenemos path valido, verificar si debemos mantenerlo
        //if (_pathLen > 0 && _pathFollower != null)
        if (_smoothedPathLen > 0 && _pathFollower != null)
        {
            int currentIdx = Mathf.Clamp(_pathFollower.CurrentIndex, 0, _smoothedPathLen - 1);
            Vector3 currentWP = _smoothedPath[currentIdx];
            float distToCurrentWP = Vector3.Distance(transform.position, currentWP);

            // a) Si estamos progresando hacia el waypoint actual, NUNCA recomputar
            if (distToCurrentWP < 10.0f) // Dentro de 10 metros del WP actual
            {
                return; // Mantener path actual
            }

            // b) Si llegamos al ultimo waypoint, no recomputar
            if (_pathFollower.ReachedEnd)
            {
                return;
            }

            // c) Verificar si estamos cerca de CUALQUIER waypoint del path
            bool nearAnyWaypoint = false;
            for (int i = 0; i < _smoothedPathLen; i++)
            {
                // Dentro de 8 metros de algun waypoint
                if (Vector3.Distance(transform.position, _smoothedPath[i]) < 8.0f)
                {
                        nearAnyWaypoint = true;
                        break;
                }
            }

            if (nearAnyWaypoint)
            {
                return; // Mantener path actual
            }

            // d) Respetar intervalo de tiempo
            float timeSinceLastCompute = now - _lastAStarTime;
            if (timeSinceLastCompute < fleeRecomputeInterval)
            {
                return; // Path aún fresco
            }

            // Si llegamos aqui, estamos MUY lejos del path -> permitir recompute
            Debug.LogWarning($"[{name}] Far from all waypoints, recomputing path");
        }

        // 2) Computar nuevo path
        int startIdx = ClosestNodeIndex(transform.position);
        if (startIdx < 0)
        {
            _pathLen = 0;
            _smoothedPathLen = 0;
            return;
        }

        // IMPORTANTE: Si ya tenemos un path, verificar que el nuevo START
        // sea diferente del actual, si no, no vale la pena recomputar
        if (_pathLen > 0 && _pathIdx != null && _pathIdx[0] == startIdx)
        {
            if (enableDebugLogs)
                Debug.Log($"[{name}] START node unchanged ({startIdx}), keeping current path");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[{name}] Computing path from node {startIdx} to {fleeTargetNodeIndex}");

        // Guardar progreso anterior
        Vector3 prevTarget = Vector3.zero;
        int prevIdx = 0;
        bool hadPath = _smoothedPathLen > 0 && _pathFollower != null;

        if (hadPath)
        {
            prevIdx = Mathf.Clamp(_pathFollower.CurrentIndex, 0, _smoothedPathLen - 1);
            prevTarget = _smoothedPath[prevIdx];
        }

        // 3) Ejecutar A*
        int newLen = AStarNoAlloc.FindPath(
            fleeGraph, startIdx, fleeTargetNodeIndex,
            _astarG, _astarF, _astarFrom, _astarClosed,
            _heapIdx, _heapF, _pathIdx
        );

        if (newLen > 0)
        {
            _pathLen = _pathFollower.BuildWorldPath(fleeGraph, _pathIdx, newLen, _worldPath);
            _lastAStarTime = now;

            if (enableDebugLogs)
            {
                string pathStr = "";
                for (int i = 0; i < Mathf.Min(5, _pathLen); i++)
                    pathStr += $"{_pathIdx[i]} ";
                if (_pathLen > 5) pathStr += "...";
                Debug.Log($"[{name}] ✓ New path: {_pathLen} waypoints [{pathStr}]");
            }

            /*// 4) Preservar progreso: encontrar waypoint más cercano al anterior
            if (hadPath)
            {
                int bestIdx = 0;
                float bestDist = float.MaxValue;

                for (int i = 0; i < _pathLen; i++)
                {
                    float dist = Vector3.Distance(_worldPath[i], prevTarget);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = i;
                    }
                }

                // Solo avanzar cursor si el nuevo waypoint está más adelante
                if (bestIdx > 0)
                {
                    _pathFollower.ReseedCursor(bestIdx);
                    if (enableDebugLogs)
                        Debug.Log($"[{name}] Preserved progress: cursor at WP {bestIdx}");
                }
            }*/
            // 4) Aplicar Smoothing
            if (usePathSmoothing && smoothingMode != PathSmoothingMode.None)
            {
                if (smoothingMode == PathSmoothingMode.Simple)
                {
                    _smoothedPathLen = PathSmoother.SmoothPathSimple(
                        _worldPath, _pathLen, _smoothedPath, transform.position,
                        obstaclesMask, personalArea, smoothingSkipStride);
                }
                else // Full
                {
                    _smoothedPathLen = PathSmoother.SmoothPath(
                        _worldPath, _pathLen, _smoothedPath, transform.position,
                        obstaclesMask, personalArea);
                }
            }
            else
            {
                // Sin smoothing, copiar raw a smoothed
                System.Array.Copy(_worldPath, _smoothedPath, _pathLen);
                _smoothedPathLen = _pathLen;
            }

            _pathFollower.ReseedCursor(0);

            if (enableDebugLogs)
                Debug.Log($"[{name}] Path computed. Raw: {_pathLen}, Smoothed: {_smoothedPathLen}");

            // 5) Preservar progreso (usando el smoothed path)
            if (hadPath)
            {
                int bestIdx = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < _smoothedPathLen; i++)
                {
                    float dist = Vector3.Distance(_smoothedPath[i], prevTarget);
                    if (dist < bestDist) { bestDist = dist; bestIdx = i; }
                }
                if (bestIdx > 0) _pathFollower.ReseedCursor(bestIdx);
            }

        }
        else
        {
            _pathLen = 0;
            _smoothedPathLen = 0;
            if (enableDebugLogs)
                Debug.LogError($"[{name}] A* failed from {startIdx} to {fleeTargetNodeIndex}");
        }
    }

    // 4) Un tick de follow-path -> steering deseado
    /*public Vector3 TickFleePathSteering()
    {
        return _pathFollower != null && _pathLen > 0
            ? _pathFollower.Tick(transform.position, CurrentVelocity, FleeSpeed, _worldPath)
            : Vector3.zero;
    }*/
    public Vector3 TickFleePathSteering()
    {
        // Pasamos _smoothedPath en lugar de _worldPath
        return _pathFollower != null && _smoothedPathLen > 0
            ? _pathFollower.Tick(transform.position, CurrentVelocity, FleeSpeed, _smoothedPath)
            : Vector3.zero;
    }

    // 5) ¿Llegue al último waypoint?
    public bool FleePathReachedEnd()
    {
        if (_pathFollower == null || _smoothedPathLen <= 0) return false;
        if (!_pathFollower.ReachedEnd) return false; // ya estamos en el ultimo waypoint

        // Chequeo de distancia final (sin sqrt)
        var goal = _smoothedPath[_smoothedPathLen - 1];
        float r2 = fleeWaypointReach * fleeWaypointReach;
        return (transform.position - goal).sqrMagnitude <= r2;
    }


    // 6) Limpiar ruta (opcional)
    public void ClearFleePath()
    {
        _pathLen = 0;
        _smoothedPathLen = 0;
    }

    /// <summary>
    /// Distancia restante (aproximada) hasta el nodo seguro actual.
    /// Devuelve PositiveInfinity si no hay path/grafo para que el caller trate el caso como "sin salida clara".
    /// </summary>
    public float GetRemainingFleeDistance()
    {
        if (_pathFollower == null || _smoothedPath == null || _smoothedPathLen <= 0)
            return float.PositiveInfinity;

        // Usamos el path suavizado
        int idx = Mathf.Clamp(_pathFollower.CurrentIndex, 0, _smoothedPathLen - 1);
        float distance = Vector3.Distance(transform.position, _smoothedPath[idx]);
        for (int i = idx; i < _smoothedPathLen - 1; i++)
        {
            distance += Vector3.Distance(_smoothedPath[i], _smoothedPath[i + 1]);
        }

        return distance;
    }

    /// <summary>
    /// Estimación de distancia directa al nodo objetivo por si aún no hay path construido.
    /// </summary>
    public float GetEstimatedDistanceToSafeNode()
    {
        if (!HasFleeGraph || fleeGraph == null || fleeTargetNodeIndex < 0 || fleeTargetNodeIndex >= fleeGraph.NodeCount)
            return float.PositiveInfinity;

        return Vector3.Distance(transform.position, fleeGraph.nodePositions[fleeTargetNodeIndex]);
    }

    #endregion

    #region FSM Management

    /// <summary>
    /// Set the current max speed for movement
    /// </summary>
    /// <param name="speed">The new max speed</param>
    public void SetCurrentMaxSpeed(float speed)
    {
        currentMaxSpeed = speed;
    }

    /// <summary>
    /// Change material color to attack color (red)
    /// </summary>
    public void SetAttackColor()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = attackColor;
        }
    }

    /// <summary>
    /// Restore original material color
    /// </summary>
    public void RestoreOriginalColor()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = originalColor;
        }
    }

    public override void TakeDamage(float damage)
    {
        if (!isAlive) return;

        float previousNorm = HealthNormalized;

        // Play hurt sound based on gender
        if (m_audioService != null && m_audioConfig != null)
        {
            AudioClip hurtSFX = gender == NPCGender.Male ? m_audioConfig.maleHurtSFX : m_audioConfig.femaleHurtSFX;
            m_audioService.PlaySFX(hurtSFX);
        }

        base.TakeDamage(damage);
        RecalculateDyingWeight();

        if (enableDebugLogs && isAlive && !Mathf.Approximately(previousNorm, HealthNormalized))
        {
            MyLogger.LogInfo($"Civilian {gameObject.name}: Health {previousNorm:P1} -> {HealthNormalized:P1}, DyingWeight={dyingRouletteWeight:F2}");
        }
    }

    protected override void OnDeath()
    {
        // Play death sound based on gender
        if (m_audioService != null && m_audioConfig != null)
        {
            AudioClip deathSFX = gender == NPCGender.Male ? m_audioConfig.maleDeathSFX : m_audioConfig.femaleDeathSFX;
            m_audioService.PlaySFX(deathSFX);
        }

        base.OnDeath();
    }

    /// <summary>
    /// Apply damage to player if available
    /// </summary>
    public void DealMeleeAttack()
    {
        if (player == null) return;

        // Play punch sound
        if (m_audioService != null && m_audioConfig != null)
        {
            m_audioService.PlaySFX(m_audioConfig.punchSFX);
        }

        // Try to get player health component
        var playerHealth = player.GetComponent<IDamageable>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(meleeDamage);

            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: Dealt {meleeDamage} melee damage to player");
        }
        else
        {
            /* Todo: Que es esto?
            // Fallback: try GameStateManager
            var gameStateManager = ServiceLocator.Get<GameStateService>();
            if (gameStateManager != null)
            {
                // gameStateManager.ApplyMeleeHit(meleeDamage);
                if (enableDebugLogs)
                    MyLogger.LogInfo($"Civilian {gameObject.name}: Applied melee hit via GameStateManager");
            }*/
        }

        // Notify decision tree that damage was dealt
        if (useDecisionTree && decisionTreeRunner != null)
        {
            decisionTreeRunner.OnMeleeDamageDealt();
        }
    }

    /// <summary>
    /// Called by FSM when an attack cycle completes (for decision tree integration)
    /// </summary>
    public void OnAttackCycleComplete()
    {
        if (useDecisionTree && decisionTreeRunner != null)
        {
            decisionTreeRunner.OnAttackCycleComplete();
            
            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: Notified DecisionTree of attack cycle completion");
        }
    }

    /// <summary>
    /// Check if pursuit should abort due to extended lose sight period
    /// This method implements the Single Responsibility pattern - 
    /// the Civilian owns the decision logic for pursuit abort
    /// </summary>
    public bool ShouldAbortPursuit()
    {
        // If we can see the player, pursuit should continue
        if (HasLoS())
        {
            return false;
        }

        // If we can't see the player, check if grace period has elapsed
        return pursuitLoseSightTimer >= attackLoseSightGrace;
    }

    /// <summary>
    /// Request a specific FSM state change (called by Decision Tree)
    /// </summary>
    public void RequestStateChange(string stateName)
    {
        if (!isAlive) return;

        if (enableDebugLogs)
            MyLogger.LogInfo($"Civilian {gameObject.name}: Decision Tree requesting state change to {stateName}");

        // Map DT suggestion to actual FSM state name
        string mappedStateName = MapDecisionTreeSuggestionToFSMState(stateName);

        // If using ScriptableObject FSM, try to change state by name
        if (useFSM && stateMachine != null)
        {
            bool stateChangeSuccess = stateMachine.ChangeStateByName(mappedStateName);
            
            if (stateChangeSuccess)
            {
                if (enableDebugLogs)
                    MyLogger.LogInfo($"Civilian {gameObject.name}: Successfully changed FSM state to {mappedStateName}");
            }
            else
            {
                if (enableDebugLogs)
                    MyLogger.LogWarning($"Civilian {gameObject.name}: Failed to find FSM state with name '{mappedStateName}'. Available states: {GetAvailableStateNames()}");
                
                // Fallback to legacy system if FSM state change fails
                RequestLegacyStateChange(stateName);
            }
        }
        else
        {
            // Use legacy state system as fallback
            RequestLegacyStateChange(stateName);
        }
    }

    /// <summary>
    /// Map Decision Tree suggestions to actual FSM state names
    /// </summary>
    private string MapDecisionTreeSuggestionToFSMState(string dtSuggestion)
    {
        switch (dtSuggestion.ToLower())
        {
            case "fleeing":
            case "flee":
                return "S_CivFlee";
                
            case "pursuing":
            case "pursue":
                return "S_CivPersuit";
                
            case "idle":
                return "S_CivIdle";
                
            case "evading":
            case "evade":
                return "S_CivEvade";
                
            case "attack":
            case "attacking":
                return "S_CivAttack";

            case "dying":
            case "limp":
                return "S_CivEvade";
                
            default:
                // Return original suggestion if no mapping found
                return dtSuggestion;
        }
    }

    /// <summary>
    /// Request a state change using the legacy state system
    /// </summary>
    private void RequestLegacyStateChange(string stateName)
    {
        CivilianState newState = currentState;

        switch (stateName.ToLower())
        {
            case "fleeing":
            case "flee":
                newState = CivilianState.Fleeing;
                break;
            case "evading":
            case "evade":
                newState = CivilianState.Evading;
                break;
            case "idle":
                newState = CivilianState.Idle;
                break;
            case "safe":
                newState = CivilianState.Safe;
                break;
            case "dying":
            case "limp":
                newState = CivilianState.Evading;
                break;
            case "pursuing":
            case "pursue":
                // Civilians don't normally pursue, but if canAttack is true, treat as fleeing for now
                newState = canAttack ? CivilianState.Fleeing : CivilianState.Fleeing;
                break;
        }

        if (newState != currentState)
        {
            ChangeState(newState);
        }
    }

    /// <summary>
    /// Check if the decision tree system is actively influencing behavior
    /// </summary>
    public bool IsDecisionTreeActive()
    {
        return useDecisionTree && decisionTreeRunner != null && decisionTreeRunner.enabled;
    }

    /// <summary>
    /// Get available state names for debugging
    /// </summary>
    private string GetAvailableStateNames()
    {
        if (stateMachine?.GetAllStates() == null)
            return "None";

        var stateNames = stateMachine.GetAllStates()
            .Where(state => state?.State?.StateName != null)
            .Select(state => state.State.StateName)
            .ToArray();

        return stateNames.Length > 0 ? string.Join(", ", stateNames) : "None";
    }

    #endregion

    #region Legacy State System Support

    /// <summary>
    /// Change legacy state (for compatibility and fallback)
    /// </summary>
    private void ChangeState(CivilianState newState)
    {
        if (currentState != newState)
        {
            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: {currentState} → {newState}");

            currentState = newState;
            stateTimer = 0f;

            // State-specific initialization
            switch (newState)
            {
                case CivilianState.Safe:
                    // Write to blackboard if civilian reaches safety
                    if (m_blackboardService != null)
                    {
                        // This could be used for global alert state
                        m_blackboardService.SetValue(BlackboardKeys.GLOBAL_ALERT, true);
                    }
                    break;
            }
        }
    }

    #endregion

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

        // Clamp velocity to current maximum speed
        if (newVel.sqrMagnitude > currentMaxSpeed * currentMaxSpeed)
        {
            newVel = newVel.normalized * currentMaxSpeed;
        }

        return newVel;
    }

    /// <summary>
    /// Apply steering force with obstacle avoidance and movement (identical to Guard)
    /// </summary>

    public void ApplySteering(Vector3 steering)
    {
        if (!isAlive) return;

        // 1) Integrar el steering del path
        Vector3 desiredVel = Integrate(steering, Time.deltaTime);
        desiredVel.y = 0f;

        float desiredSpeed = desiredVel.magnitude;
        if (desiredSpeed <= 0.0001f) return;

        Vector3 desiredDir = desiredVel / Mathf.Max(desiredSpeed, 1e-5f);

        // 2) Obstacle avoidance - SOLO ajuste lateral, NO retroceso
        Vector3 avoidedVel = obstacleAvoidance.GetDirImproved(desiredVel, false);
        Vector3 avoidanceDelta = avoidedVel - desiredVel;
        Vector3 avoidDir = avoidanceDelta.sqrMagnitude > 1e-6f ? avoidanceDelta.normalized : Vector3.zero;

        // 3) Blend adaptativo - permitir retroceder cuando la pared esta enfrente
        float pathW = 1.0f;
        float avoidW = 0.35f;

        if (avoidDir != Vector3.zero)
        {
            float oppositeFactor = Mathf.Clamp01(-Vector3.Dot(avoidDir, desiredDir));
            // Mas oposicion => mas peso para separarnos de la pared
            float weightBoost = Mathf.Lerp(0f, 0.75f, oppositeFactor);
            avoidW += weightBoost;

            Color debugColor = Color.Lerp(Color.yellow, Color.red, oppositeFactor);
            Debug.DrawRay(transform.position, avoidDir * 2f, debugColor, 0.1f);
        }

        // Blend: path + corrección (ahora puede empujar hacia atras)
        Vector3 blended = (desiredVel * pathW) + (avoidDir * (avoidW * currentMaxSpeed));

        // 4) Clamp manteniendo direccion del path
        float maxV = currentMaxSpeed;
        if (blended.sqrMagnitude > maxV * maxV)
        {
            // IMPORTANTE: No normalizar ciegamente, mantener bias hacia el path
            // Si el blended excede la velocidad, recortar la componente de avoidance primero
            Vector3 pathComponent = Vector3.Project(blended, desiredDir);
            Vector3 avoidComponent = blended - pathComponent;

            // Recortar avoidance si es necesario
            float pathMag = pathComponent.magnitude;
            float avoidMag = avoidComponent.magnitude;
            float totalMag = Mathf.Sqrt(pathMag * pathMag + avoidMag * avoidMag);

            if (totalMag > maxV)
            {
                // Priorizar path, recortar avoidance
                float scale = Mathf.Sqrt(Mathf.Max(0, maxV * maxV - pathMag * pathMag)) / Mathf.Max(avoidMag, 1e-5f);
                avoidComponent *= Mathf.Min(scale, 1f);
                blended = pathComponent + avoidComponent;
            }
        }

        blended.y = 0f;
        _vel = blended;

        // 5) Aplicar movimiento manteniendo Y
        if (_vel.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = transform.position + _vel * Time.deltaTime;
            newPos.y = transform.position.y; // Mantener altura
            transform.position = newPos;

            currentMovementDirection = _vel.normalized;
            currentMovementSpeed = _vel.magnitude;

            // Debug: mostrar velocidad final
            Debug.DrawRay(transform.position, _vel, Color.cyan, 0.1f);

            if (_vel.sqrMagnitude > 0.01f)
            {
                Vector3 look = _vel.normalized;
                look.y = 0f;
                transform.rotation = Quaternion.LookRotation(look);
            }
        }
    }

    public void ApplySteeringFlee(Vector3 steering)
    {
        if (!isAlive) return;

        // 1) Integrar steering del path
        Vector3 desiredVel = Integrate(steering, Time.deltaTime);
        desiredVel.y = 0f;

        // Clamp velocidad
        float maxV = currentMaxSpeed;
        if (desiredVel.sqrMagnitude > maxV * maxV)
            desiredVel = desiredVel.normalized * maxV;

        Vector3 finalVel = desiredVel;

        // 2) OBSTACLE AVOIDANCE MEJORADO
        // Usamos SphereCast para "ver" el volumen del NPC hacia adelante
        float lookAheadDist = Mathf.Max(2f, currentMovementSpeed * 0.5f); // Mirar más lejos si va rápido

        // Si detectamos algo en nuestra trayectoria futura...
        if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, personalArea, desiredVel.normalized, out RaycastHit hit, lookAheadDist, obstaclesMask))
        {
            // ...Usamos ObstacleAvoidance para calcular una ruta de escape
            // Esto aprovecha tu lógica de GetDirImproved o GetDir
            Vector3 avoidanceDir = obstacleAvoidance.GetDir(desiredVel, false);

            // Calculamos que tan urgente es girar (0 = lejos, 1 = colision inminente)
            float danger = 1f - (hit.distance / lookAheadDist);

            // Interpolamos agresivamente segun el peligro (entre 30% y 100% de fuerza de evasion)
            float blendStrength = Mathf.Lerp(0.3f, 1.0f, danger * danger);

            finalVel = Vector3.Lerp(desiredVel, avoidanceDir.normalized * currentMaxSpeed, blendStrength);

            // Debug visual
            Debug.DrawLine(transform.position, hit.point, Color.red);
            Debug.DrawRay(transform.position, finalVel, Color.magenta);
        }

        finalVel.y = 0f;
        _vel = finalVel;

        // 3) Aplicar movimiento
        if (_vel.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = transform.position + _vel * Time.deltaTime;
            // Check simple para no atravesar piso si hay desniveles
            newPos.y = transform.position.y;
            transform.position = newPos;

            currentMovementDirection = _vel.normalized;
            currentMovementSpeed = _vel.magnitude;

            if (_vel.sqrMagnitude > 0.01f)
            {
                Vector3 look = _vel.normalized;
                look.y = 0f;
                transform.rotation = Quaternion.LookRotation(look);
            }
        }
    }

    public void ApplySteeringDebug(Vector3 steering)
    {
        if (!isAlive) return;

        // VERSION SIMPLIFICADA PARA DEBUG - SIN OBSTACLE AVOIDANCE
        Vector3 desiredVel = Integrate(steering, Time.deltaTime);
        desiredVel.y = 0f;

        float maxV = currentMaxSpeed;
        if (desiredVel.sqrMagnitude > maxV * maxV)
            desiredVel = desiredVel.normalized * maxV;

        _vel = desiredVel;

        if (_vel.sqrMagnitude > 0.001f)
        {
            transform.position += _vel * Time.deltaTime;

            Debug.DrawRay(transform.position, _vel, Color.cyan, 0.1f); // Ver velocidad final

            currentMovementDirection = _vel.normalized;
            currentMovementSpeed = _vel.magnitude;

            if (_vel.sqrMagnitude > 0.01f)
            {
                Vector3 look = _vel.normalized;
                look.y = 0f;
                transform.rotation = Quaternion.LookRotation(look);
            }
        }
    }

    /// <summary>
    /// Steering especializado para pathfinding flee - prioriza seguir el path sobre todo
    /// </summary>
    public void ApplySteeringFleeOriginal(Vector3 steering)
    {
        if (!isAlive) return;

        // 1) Integrar steering del path (igual que Debug)
        Vector3 desiredVel = Integrate(steering, Time.deltaTime);
        desiredVel.y = 0f;

        float maxV = currentMaxSpeed;
        if (desiredVel.sqrMagnitude > maxV * maxV)
            desiredVel = desiredVel.normalized * maxV;

        // 2) Obstacle avoidance MUY SUAVE - solo para evitar colisiones directas
        // No usar GetDirImproved que es muy agresivo, solo detectar colision inminente
        Vector3 finalVel = desiredVel;

        // Raycast corto hacia adelante para detectar colisión DIRECTA
        Vector3 checkDir = desiredVel.normalized;
        float checkDist = personalArea * 1.5f; // Muy corto, solo colisiones inmediatas

        if (Physics.Raycast(transform.position, checkDir, out RaycastHit hit, checkDist, obstaclesMask))
        {
            // Colision inminente - ajuste lateral MINIMO
            Vector3 normal = hit.normal;
            normal.y = 0f;

            if (normal.sqrMagnitude > 0.01f)
            {
                normal.Normalize();

                // Proyectar velocidad deseada al plano del obstaculo (deslizar)
                Vector3 slideVel = Vector3.ProjectOnPlane(desiredVel, normal);

                // Blend muy suave: 90% original, 10% slide
                finalVel = Vector3.Lerp(desiredVel, slideVel, 0.1f);

                Debug.DrawRay(hit.point, normal, Color.red, 0.1f);
                Debug.DrawRay(transform.position, slideVel.normalized, Color.yellow, 0.1f);
            }
        }

        finalVel.y = 0f;
        _vel = finalVel;

        // 3) Aplicar movimiento (igual que Debug)
        if (_vel.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = transform.position + _vel * Time.deltaTime;
            newPos.y = transform.position.y;
            transform.position = newPos;

            currentMovementDirection = _vel.normalized;
            currentMovementSpeed = _vel.magnitude;

            Debug.DrawRay(transform.position, _vel, Color.cyan, 0.1f);

            if (_vel.sqrMagnitude > 0.01f)
            {
                Vector3 look = _vel.normalized;
                look.y = 0f;
                transform.rotation = Quaternion.LookRotation(look);
            }
        }
    }


    #endregion

    #region Detection

    /// <summary>
    /// Check if civilian has line of sight to player
    /// </summary>
    public bool HasLoS()
    {
        if (player == null || playerDetector == null) return false;

        // Force visibility if player is in melee range to avoid LoS flickering during attacks
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= meleeRange)
        {
            if (enableDebugLogs)
                MyLogger.LogInfo($"Civilian {gameObject.name}: Forcing LoS=true (in melee range: {distanceToPlayer:F2} <= {meleeRange})");
            return true;
        }

        // Get debug info which includes hasLOS (line of sight) information
        var debugInfo = playerDetector.GetDebugInfo();

        // Additional distance check
        bool inRange = distanceToPlayer <= sightRange;

        // Additional FOV check
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 forward = transform.forward;
        float angle = Vector3.Angle(forward, directionToPlayer);
        bool inFOV = angle <= sightFOV * 0.5f;

        // Use hasLOS specifically - true line of sight with no obstacles
        return debugInfo.hasLOS && inRange && inFOV;
    }

    /// <summary>
    /// Get distance to player
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    /// <summary>
    /// Check if player is in melee range
    /// </summary>
    public bool IsPlayerInMeleeRange()
    {
        return GetDistanceToPlayer() <= meleeRange;
    }

    #endregion

    #region BaseCharacter Implementation

    public override void Move(Vector3 direction)
    {
        if (!isAlive) return;

        // Direct movement using steering
        Vector3 targetVel = direction.normalized * currentMaxSpeed;
        Vector3 steering = targetVel - _vel;
        ApplySteering(steering);
    }

    public override void Shoot(Vector3 direction)
    {
        if (!isAlive || !canAttack || !CanShoot()) return;

        // Basic shooting implementation
        lastShootTime = Time.time;
        if (enableDebugLogs)
            MyLogger.LogInfo($"{gameObject.name}: Civilian shooting at {direction}");
        // TODO: Implement actual shooting logic when canAttack is enabled
    }

    #endregion

    #region IUpdatable Implementation

    public bool IsActive => isAlive && gameObject.activeInHierarchy;

    #endregion

    #region IUseFsm Implementation

    public Transform GetModelTransform()
    {
        return transform;
    }

    public void UpdateFsm()
    {
        if (stateMachine != null)
        {
            stateMachine.RunStateMachine();
        }
    }

    public void SetTargetTransform(Transform p_target)
    {
        player = p_target;
        
        // Update AI system when target changes
        if (m_blackboardService != null && player != null)
        {
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, player);
            m_blackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, player.position);
        }
    }

    public Transform GetTargetTransform()
    {
        return player;
    }

    #endregion

    #region Dying Support

    private void RecalculateDyingWeight()
    {
        float healthLoss = 1f - HealthNormalized;
        float curveValue = dyingWeightCurve != null && dyingWeightCurve.length > 0
            ? Mathf.Clamp01(dyingWeightCurve.Evaluate(healthLoss))
            : healthLoss;

        dyingRouletteWeight = Mathf.Clamp(curveValue * dyingWeightMax, 0f, dyingWeightMax);
    }

    #endregion

    #region Debug and Gizmos

    private void OnDrawGizmos()
    {
        if (!enabled) return;

        // Sight range and FOV
        Gizmos.color = currentState == CivilianState.Fleeing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // FOV cone
        if (sightFOV < 360f)
        {
            Vector3 leftBoundary = Quaternion.Euler(0, -sightFOV * 0.5f, 0) * transform.forward * sightRange;
            Vector3 rightBoundary = Quaternion.Euler(0, sightFOV * 0.5f, 0) * transform.forward * sightRange;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }

        // Safe distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance);

        // Melee range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Current velocity
        if (_vel.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _vel);
        }

        // Last known player position
        if (hasEverSeenPlayer && lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireCube(lastKnownPlayerPosition, Vector3.one * 0.5f);
        }
    }

    [ContextMenu("Debug Civilian Status")]
    private void DebugCivilianStatus()
    {
        Debug.Log("=== CIVILIAN STATUS ===");
        Debug.Log($"Using ScriptableObject FSM: {useFSM}");
        Debug.Log($"Using Decision Tree: {useDecisionTree}");
        
        if (useFSM && stateMachine != null)
        {
            var currentState = stateMachine.GetCurrentState();
            Debug.Log($"Current State: {(currentState?.State?.StateName ?? "None")}");
            Debug.Log($"ScriptableObject FSM Active: True");
            Debug.Log($"Available FSM States: {GetAvailableStateNames()}");
        }
        else
        {
            Debug.Log($"Legacy State: {currentState}");
            Debug.Log($"ScriptableObject FSM Active: False");
        }
        
        Debug.Log($"State Timer: {stateTimer:F2}s");
        Debug.Log($"Safe Timer: {safeTimer:F2}s");
        Debug.Log($"Can See Player: {HasLoS()}");
        Debug.Log($"Distance to Player: {GetDistanceToPlayer():F2}");
        Debug.Log($"Current Velocity: {_vel.magnitude:F2}");
        Debug.Log($"Current Max Speed: {currentMaxSpeed:F2}");
        Debug.Log($"Has Ever Seen Player: {hasEverSeenPlayer}");
        Debug.Log($"Last Known Player Pos: {lastKnownPlayerPosition}");
        Debug.Log($"Can Attack: {canAttack}");
        
        // Decision Tree status
        if (useDecisionTree && decisionTreeRunner != null)
        {
            Debug.Log($"Decision Tree Active: {decisionTreeRunner.enabled}");
            Debug.Log($"DT Status: {decisionTreeRunner.GetStatus()}");
            Debug.Log($"DT Last Suggestion: {decisionTreeRunner.LastSuggestion}");
        }
        else
        {
            Debug.Log($"Decision Tree Active: False");
        }
        
        Debug.Log("=======================");
    }

    // Getter for pathfinding
    public int PathLenForDebug => _pathLen;
    public int SmoothedPathLenForDebug => _smoothedPathLen;
    public PathFollowerAgent PathFollower => _pathFollower;
    public Vector3[] WorldPathForDebug => _worldPath;
    public Vector3[] SmoothedPathForDebug => _smoothedPath;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        using (new UnityEditor.Handles.DrawingScope())
        {
            // === DIAGNOSTICO DE GRAFO ===
            if (fleeGraph != null && fleeGraph.NodeCount > 0)
            {
                bool validTarget = fleeTargetNodeIndex >= 0 && fleeTargetNodeIndex < fleeGraph.NodeCount;

                // === NODO START (mas cercano) ===
                int startIdx = ClosestNodeIndex(transform.position);
                if (startIdx >= 0)
                {
                    Vector3 startPos = fleeGraph.nodePositions[startIdx];

                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(startPos, 0.2f);
                    Gizmos.DrawWireSphere(startPos, 0.3f);

                    UnityEditor.Handles.Label(
                        startPos + Vector3.up * 0.5f,
                        $"START\n[{startIdx}]",
                        new GUIStyle()
                        {
                            normal = new GUIStyleState() { textColor = Color.green },
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter
                        }
                    );

                    // Linea desde NPC a START
                    UnityEditor.Handles.color = new Color(0, 1, 0, 0.5f);
                    UnityEditor.Handles.DrawDottedLine(transform.position, startPos, 3f);
                }

                // === NODO GOAL (objetivo final) ===
                if (validTarget)
                {
                    Vector3 goalPos = fleeGraph.nodePositions[fleeTargetNodeIndex];

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(goalPos, 0.25f);
                    Gizmos.DrawWireSphere(goalPos, fleeWaypointReach);

                    UnityEditor.Handles.Label(
                        goalPos + Vector3.up * 0.5f,
                        $"SAFE\n[{fleeTargetNodeIndex}]",
                        new GUIStyle()
                        {
                            normal = new GUIStyleState() { textColor = Color.yellow },
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter
                        }
                    );
                }

                // =========================================================
                // 1) Dibujar Path RAW (A* Original) - GRIS TENUE
                // =========================================================
                if (_pathLen > 0 && _worldPath != null)
                {
                    UnityEditor.Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gris transparente

                    // Copiamos solo los nodos validos
                    Vector3[] rawPts = new Vector3[_pathLen];
                    System.Array.Copy(_worldPath, rawPts, _pathLen);

                    UnityEditor.Handles.DrawAAPolyLine(2.0f, rawPts);

                    // Opcional: Pequeños puntos para ver los nodos originales
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    foreach (var p in rawPts) Gizmos.DrawSphere(p, 0.05f);
                }

                // =========================================================
                // 2) Dibujar Path SUAVIZADO (String Pulling) - CYAN BRILLANTE
                // =========================================================
                if (_smoothedPathLen > 0 && _smoothedPath != null)
                {
                    Vector3[] smPts = new Vector3[_smoothedPathLen];
                    System.Array.Copy(_smoothedPath, smPts, _smoothedPathLen);

                    // Linea gruesa
                    UnityEditor.Handles.color = new Color(0f, 0.8f, 1f, 1f);
                    UnityEditor.Handles.DrawAAPolyLine(5.0f, smPts);

                    for (int i = 0; i < _smoothedPathLen; i++)
                    {
                        Vector3 wp = _smoothedPath[i];
                        bool isCurrent = (_pathFollower != null && _pathFollower.CurrentIndex == i);

                        // Color: Verde si es el actual, Azul celeste si no
                        Gizmos.color = isCurrent ? Color.green : new Color(0, 0.7f, 1f);
                        float size = isCurrent ? 0.3f : 0.15f;

                        Gizmos.DrawSphere(wp, size);

                        // Etiqueta para el waypoint actual
                        if (isCurrent)
                        {
                            Gizmos.DrawWireSphere(wp, fleeWaypointReach);
                            UnityEditor.Handles.Label(wp + Vector3.up * 0.5f, "TARGET", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.green} });
                        }
                    }

                    // =========================================================
                    // 3) TEXTO DE INFORMACION (Actualizado para leer Smoothed Path)
                    // =========================================================
                    GUIStyle pathInfoStyle = new GUIStyle();
                    pathInfoStyle.normal.textColor = Color.cyan;
                    pathInfoStyle.fontSize = 11;
                    pathInfoStyle.fontStyle = FontStyle.Bold;
                    pathInfoStyle.alignment = TextAnchor.UpperLeft;

                    // Calculamos reduccion
                    float reduction = _pathLen > 0 ? (1f - (float)_smoothedPathLen / _pathLen) * 100f : 0f;

                    string pathInfo = $"PATH DATA:\n" +
                                      $"• Raw Nodes: {_pathLen}\n" +
                                      $"• Smooth Nodes: {_smoothedPathLen} (-{reduction:F0}%)\n" +
                                      $"• Current WP Idx: {_pathFollower?.CurrentIndex ?? -1}";

                    // Distancia al waypoint ACTUAL (del path suavizado)
                    if (_pathFollower != null && _pathFollower.CurrentIndex < _smoothedPathLen)
                    {
                        Vector3 currentWP = _smoothedPath[_pathFollower.CurrentIndex];
                        float distToCurrent = Vector3.Distance(transform.position, currentWP);
                        pathInfo += $"\n• Dist to WP: {distToCurrent:F2}m";

                        // Linea punteada al objetivo actual
                        UnityEditor.Handles.color = new Color(0.2f, 1f, 0.2f, 0.5f);
                        UnityEditor.Handles.DrawDottedLine(transform.position, currentWP, 5f);
                    }

                    UnityEditor.Handles.Label(
                        transform.position + Vector3.up * 1.5f + Vector3.right * 0.5f,
                        pathInfo,
                        pathInfoStyle
                    );
                }
                else if (validTarget)
                {
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, "!!! NO PATH COMPUTED", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                }
            }
            else
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "!!! NO FLEE GRAPH", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } });
            }

            // === VELOCIDAD ACTUAL ===
            if (_vel.magnitude > 0.1f)
            {
                UnityEditor.Handles.color = Color.magenta;
                UnityEditor.Handles.DrawAAPolyLine(4f, transform.position, transform.position + _vel);
            }
        }
    }
#endif

    #endregion

    #region DebugPathfinding

    [ContextMenu("Debug Graph Info")]
    void DebugGraphInfo()
    {
        if (fleeGraph == null)
        {
            Debug.LogError($"[{name}] fleeGraph is NULL!");
            return;
        }

        Debug.Log($"=== GRAPH INFO [{name}] ===");
        Debug.Log($"Node count: {fleeGraph.NodeCount}");
        Debug.Log($"Target index: {fleeTargetNodeIndex}");
        Debug.Log($"Target valid: {fleeTargetNodeIndex >= 0 && fleeTargetNodeIndex < fleeGraph.NodeCount}");

        // Mostrar primeros 5 nodos
        for (int i = 0; i < Mathf.Min(5, fleeGraph.NodeCount); i++)
        {
            var neighbors = fleeGraph.neighbors[i].data;
            Debug.Log($"  Node {i}: pos={fleeGraph.nodePositions[i]}, neighbors={neighbors.Length} [{string.Join(",", neighbors)}]");
        }

        // Verificar conectividad del target
        if (fleeTargetNodeIndex >= 0 && fleeTargetNodeIndex < fleeGraph.NodeCount)
        {
            var targetNeighbors = fleeGraph.neighbors[fleeTargetNodeIndex].data;
            Debug.Log($"  Target node {fleeTargetNodeIndex}: neighbors={targetNeighbors.Length}");
        }

        Debug.Log("=======================");
    }

    #endregion

    #region Cleanup

    private void OnDisable()
    {
        UnsubscribeUpdateService();
    }

    public void MyUpdate()
    {
        if (!isAlive) return;

        // Update timers
        stateTimer += Time.deltaTime;

        // Use ScriptableObject FSM if enabled, otherwise fallback to legacy system
        if (useFSM && stateMachine != null)
        {
            UpdateFsm();
        }
        else if (!useDecisionTree)
        {
            // Use legacy behavior system only if decision tree is disabled
            // UpdateBehavior(); // Commented out - legacy system replaced by FSM/DT
        }

        // Note: Decision Tree runs independently via its own coroutine
        // and influences behavior through RequestStateChange calls
    }

    public void SubscribeUpdateService()
    {
        ServiceLocator.Get<IUpdateService>().AddUpdateListener(this);
    }

    public void UnsubscribeUpdateService()
    {
        ServiceLocator.Get<IUpdateService>().RemoveUpdateListener(this);
    }

    #endregion
}
