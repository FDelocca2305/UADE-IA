using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Services.MicroServices.BlackboardService;
using DevelopmentUtilities;
using Services;

/// <summary>
/// Decision Tree runner for Civilian NPCs that evaluates behavioral suggestions at runtime.
/// The tree suggests actions (Flee, Alert, Resume) to the Civilian FSM without directly moving transforms.
/// All movement continues through ApplySteering → ObstacleAvoidance system.
/// </summary>
public class CivilianDecisionTreeRunner : MonoBehaviour
{
    private enum CivilianStance
    {
        Escape,
        Attack,
        Dying
    }
    
    [Header("Decision Tree Configuration")]
    [SerializeField] private float evaluationInterval = 0.15f;  // Reduced frequency now that we have locks/hysteresis
    [SerializeField] private float alertChanceWhenNoLoS = 0.5f;
    [SerializeField] private string resumeSuggestion = "Idle";
    [SerializeField] private float alertCooldown = 2f;     // Minimum time between alert triggers
    
    [Header("Attack Configuration")]
    [SerializeField] private float postHitFleeTime = 1.2f;  // Time to flee after landing a hit (hit-and-run)
    
    [Header("Debug")]
    [SerializeField] private bool debugDT = true;  // Enable debug to see roulette working
    
    // Decision Tree components
    private ITreeNode m_root;
    
    // Component references
    private Civilian m_civilian;
    
    // State tracking
    private string m_currentSuggestion = "";
    private string m_lastSuggestion = "";
    private float m_lastEvaluationTime = 0f;
    private float m_lastAlertTime = -1f;      // Last time an alert was triggered

    // Decision Tree timing logic (moved from FSM states)
    private float m_evadeStartTime = 0f;      // When evade state was entered
    private float m_fleeStartTime = 0f;       // When flee state was entered
    private float m_safeTimer = 0f;           // Time spent in safe conditions
    private float m_pursuitStartTime = 0f;    // When pursuit was started (for minimum commitment)

    // Separated LoS timers to avoid overlap/conflicts
    private float m_pursuitLoseSightTimerVisible = 0f;   // Timer for breaking stance lock when LoS lost in visible branch
    private float m_pursuitLoseSightTimerInvisible = 0f; // Timer for commitment when LoS lost in invisible branch
    private float m_timeWithoutLoS = 0f;                 // Tiempo acumulado sin LoS (para decaimiento de intención)
    private Vector3 m_lastKnownPlayerPosition = Vector3.zero;

    // Attack cycle tracking
    private bool m_isInAttackCycle = false;   // Whether we're in a non-interruptible attack cycle
    private float m_attackCycleStartTime = 0f; // When current attack cycle started
    private bool m_postHitFleeActive = false; // Whether we're in post-hit flee period
    private float m_postHitFleeStartTime = 0f; // When post-hit flee started

    // Stance Lock system - prevents roulette flip-flop
    private CivilianStance m_currentStance = CivilianStance.Escape;
    private float m_stanceLockUntil = 0f;     // Time until stance lock expires
    private float m_stanceLockDuration = 2.5f; // How long to maintain stance (2.5 seconds)
    private bool m_hasActiveStanceLock = false;
    private CivilianStance m_cachedEvaluationStance = CivilianStance.Escape;
    private bool m_hasCachedStanceEvaluation = false;
    
    // Coroutine reference
    private Coroutine m_evaluationCoroutine;
    
    private static IBlackboardService BlackboardService => ServiceLocator.Get<IBlackboardService>();

    #region Properties
    
    /// <summary>
    /// Last suggestion made by the decision tree (read-only for diagnostics)
    /// </summary>
    public string LastSuggestion => m_lastSuggestion;
    
    /// <summary>
    /// Current suggestion from the decision tree
    /// </summary>
    public string CurrentSuggestion => m_currentSuggestion;
    
    /// <summary>
    /// Whether debug logging is enabled
    /// </summary>
    public bool DebugEnabled => debugDT;
    
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Get required components
        m_civilian = GetComponent<Civilian>();
        if (m_civilian == null)
        {
            MyLogger.LogError($"CivilianDecisionTreeRunner on {gameObject.name}: No Civilian component found!");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        BuildDecisionTree();
        StartEvaluationLoop();
        
        if (debugDT)
            MyLogger.LogInfo($"CivilianDecisionTreeRunner on {gameObject.name}: Decision tree initialized and evaluation started");
    }

    private void OnDisable()
    {
        StopEvaluationLoop();
    }

    private void OnDestroy()
    {
        StopEvaluationLoop();
    }

    #endregion

    #region Decision Tree Construction

    private void BuildDecisionTree()
    {
        // Enhanced decision tree with timing logic and attack cycle management:
        // Root: IsPlayerVisible?
        //   YES → Check melee range → Attack/Pursue/Flee based on stance and range
        //   NO → Check for timeout conditions and alert triggers


// Create leaf nodes
var l_fleeNode = new ActionNode(() => SuggestFlee());
var l_pursueNode = new ActionNode(() => SuggestPursue());
var l_attackNode = new ActionNode(() => SuggestAttack());
var l_evadeNode = new ActionNode(() => SuggestEvade());
var l_idleNode = new ActionNode(() => SuggestIdle());
var l_alertNode = new ActionNode(() => SuggestAlert());
var l_dyingNode = new ActionNode(() => SuggestDying());

// Decision logic when player is visible
// Priority: Attack cycle (non-interruptible) -> Dying stance -> Melee range -> Roulette choice
var l_visibleDecisionNode = new QuestionNode(
    () => IsInNonInterruptibleAttackCycle(),
    l_attackNode,  // Continue attack cycle if non-interruptible
    new QuestionNode(
        () => GetEvaluatedStance() == CivilianStance.Dying,
        l_dyingNode,
        new QuestionNode(
            () => IsPlayerInMeleeRange() && m_civilian.CanAttack && GetEvaluatedStance() == CivilianStance.Attack,
            l_attackNode,  // Enter attack if in melee and stance is ATTACK
            new QuestionNode(
                () => GetEvaluatedStance() == CivilianStance.Attack,
                l_pursueNode,  // ATTACK stance but not in melee -> pursue
                l_fleeNode     // ESCAPE stance -> flee
            )
        )
    )
);

// Decision logic when player is not visible - NEVER suggest flee without LoS
// Exceptions: Continue current action if within commitment/grace period
var l_invisibleDecisionNode = new QuestionNode(
    () => IsInNonInterruptibleAttackCycle(),
    l_attackNode,  // Continue attack cycle even without LoS (brief window)
    new QuestionNode(
        () => GetEvaluatedStance() == CivilianStance.Dying,
        l_dyingNode,
        new QuestionNode(
            () => IsCurrentlyPursuing() && IsWithinPursuitCommitment(),
            l_pursueNode,  // Continue pursuing if committed (hysteresis to prevent jitter)
            new QuestionNode(
                () => IsCurrentlyFleeing() && !ShouldReturnToIdle(),
                l_fleeNode,  // Continue fleeing if already fleeing and grace timer hasn't expired
                new QuestionNode(
                    () => ShouldTriggerAlert(),
                    l_alertNode,
                    l_idleNode  // No LoS = Return to idle (SafeDistance only stops fleeing, doesn't start it)
                )
            )
        )
    )
);

// Create root node - check if player is visible
m_root = new QuestionNode(
    () => {
        bool l_hasLoS = m_civilian.HasLoS();
        return l_hasLoS;
    },
    l_visibleDecisionNode,
    l_invisibleDecisionNode
);
        if (debugDT)
            MyLogger.LogInfo($"CivilianDecisionTreeRunner on {gameObject.name}: Enhanced decision tree with timing logic built");
    }

    #endregion

    #region Decision Logic

    /// <summary>
    /// Check if civilian is in a non-interruptible attack cycle
    /// </summary>
    private bool IsInNonInterruptibleAttackCycle()
    {
        if (!m_isInAttackCycle) return false;

        // Calculate total attack cycle duration
        float l_totalAttackDuration = m_civilian.AttackWindup + m_civilian.AttackHitWin + m_civilian.AttackRecover;
        float l_timeSinceAttackStart = Time.time - m_attackCycleStartTime;

        bool l_stillInCycle = l_timeSinceAttackStart < l_totalAttackDuration;

        if (debugDT && l_stillInCycle)
            MyLogger.LogInfo($"Non-interruptible attack cycle: {l_timeSinceAttackStart:F2}s / {l_totalAttackDuration:F2}s");

        return l_stillInCycle;
    }

    /// <summary>
    /// Check if player is in melee range for immediate attack
    /// </summary>
    private bool IsPlayerInMeleeRange()
    {
        if (m_civilian == null) return false;
        return m_civilian.IsPlayerInMeleeRange();
    }

    /// <summary>
    /// Start an attack cycle - marks as non-interruptible
    /// </summary>
    private void StartAttackCycle()
    {
        m_isInAttackCycle = true;
        m_attackCycleStartTime = Time.time;
        m_postHitFleeActive = false; // Reset any previous post-hit flee

        if (debugDT)
            MyLogger.LogInfo($"Started non-interruptible attack cycle at {Time.time:F2}");
    }

    /// <summary>
    /// End attack cycle and optionally start post-hit flee
    /// </summary>
    private void EndAttackCycle(bool p_startPostHitFlee = true)
    {
        m_isInAttackCycle = false;

        if (p_startPostHitFlee)
        {
            // Solo activar hit-and-run si estamos cerca del safe node (tiene sentido huir)
            float remainingDistance = m_civilian != null ? m_civilian.GetRemainingFleeDistance() : float.PositiveInfinity;
            bool nearSafeNode = !float.IsInfinity(remainingDistance) && remainingDistance <= m_civilian.SafeDistance;

            if (nearSafeNode)
            {
                m_postHitFleeActive = true;
                m_postHitFleeStartTime = Time.time;
                
                // Break stance lock y forzar ESCAPE
                BreakStanceLock("Post-hit flee - forcing hit-and-run");
                LockStance(CivilianStance.Escape, "Post-hit flee - near safe node");

                if (debugDT)
                    MyLogger.LogInfo($"Ended attack cycle, post-hit flee near safe node (dist {remainingDistance:F1}, safe {m_civilian.SafeDistance:F1})");
            }
            else if (debugDT)
            {
                MyLogger.LogInfo("Ended attack cycle, staying in offensive posture (far from safe node)");
            }
        }
        else if (debugDT)
        {
            MyLogger.LogInfo($"Ended attack cycle without post-hit flee");
        }
    }

    /// <summary>
    /// Check if we're in post-hit flee period
    /// </summary>
    private bool IsInPostHitFlee()
    {
        if (!m_postHitFleeActive) return false;

        float l_timeSincePostHit = Time.time - m_postHitFleeStartTime;
        bool l_stillFleeing = l_timeSincePostHit < postHitFleeTime;

        if (!l_stillFleeing && m_postHitFleeActive)
        {
            m_postHitFleeActive = false;
            if (debugDT)
                MyLogger.LogInfo($"Post-hit flee period ended after {l_timeSincePostHit:F2}s");
        }

        return l_stillFleeing;
    }

    /// <summary>
    /// Evaluate roulette stance (Attack/Escape/Dying) with caching per tick
    /// </summary>
    private CivilianStance GetEvaluatedStance()
    {
        if (!m_hasCachedStanceEvaluation)
        {
            m_cachedEvaluationStance = EvaluateStance();
            m_hasCachedStanceEvaluation = true;
        }

        return m_cachedEvaluationStance;
    }

    private CivilianStance EvaluateStance()
    {
        bool hasLoS = m_civilian.HasLoS();
        UpdateLoSTimer(hasLoS);

        // Solo tirar la ruleta cuando hay LoS; de lo contrario mantener la postura actual
        if (!hasLoS)
        {
            return m_currentStance;
        }

        if (IsInPostHitFlee())
        {
            if (debugDT)
                MyLogger.LogInfo($"Civilian {m_civilian.name}: Post-hit flee active - forcing ESCAPE stance");

            LockStance(CivilianStance.Escape, "Post-hit flee cooldown");
            return CivilianStance.Escape;
        }

        if (m_hasActiveStanceLock && Time.time < m_stanceLockUntil)
        {
            m_civilian.SetDyingEvadeMode(m_currentStance == CivilianStance.Dying);
            return m_currentStance;
        }

        if (m_hasActiveStanceLock && Time.time >= m_stanceLockUntil)
        {
            BreakStanceLock("Stance lock expired");
        }

        // Pesos 100% dinámicos (sin depender del inspector)
        ComputeDynamicCoreWeights(out float attackWeight, out float escapeWeight);

        bool canAttack = m_civilian.CanAttack;
        if (!canAttack)
            attackWeight = 0f;

        float dyingWeight = Mathf.Max(0f, m_civilian.DyingWeight);

        if (!canAttack && dyingWeight <= 0.0001f)
        {
            m_civilian.SetDyingEvadeMode(false);
            return CivilianStance.Escape;
        }

        var l_decisions = new Dictionary<CivilianStance, float>
        {
            { CivilianStance.Attack, attackWeight },
            { CivilianStance.Escape, escapeWeight },
            { CivilianStance.Dying, dyingWeight }
        };

        float total = attackWeight + escapeWeight + dyingWeight;
        if (total <= 0.0001f)
        {
            m_civilian.SetDyingEvadeMode(false);
            return CivilianStance.Escape;
        }

        CivilianStance choice = RouletteWheel<CivilianStance>.Run(l_decisions);

        LogDT($"NEW ROULETTE: Choice={choice}, Attack={attackWeight:F2}, Escape={escapeWeight:F2}, Dying={dyingWeight:F2}");

        if (choice == CivilianStance.Escape && !m_civilian.CanAttack)
        {
            m_civilian.SetDyingEvadeMode(false);

            if (debugDT)
                MyLogger.LogInfo($"Civilian {m_civilian.name}: Roulette -> ESCAPE (no attack capability) - skipping stance lock");

            return CivilianStance.Escape;
        }

        LockStance(choice, $"New roulette: {choice}");

        if (choice != CivilianStance.Dying)
        {
            m_civilian.SetDyingEvadeMode(false);
        }

        if (debugDT)
            MyLogger.LogInfo($"Civilian {m_civilian.name}: Stance -> {choice}, locked for {m_stanceLockDuration}s");

        return choice;
    }

    /// <summary>
    /// Calcula pesos Attack/Escape de forma dinámica (sin inspector).
    /// Lejos del safe node y con LoS: favorece Attack; cerca: favorece Escape.
    /// Sin path: se asume sin escape claro → favorece Attack.
    /// </summary>
    private void ComputeDynamicCoreWeights(out float attackWeight, out float escapeWeight)
    {
        attackWeight = 1f;
        escapeWeight = 1f;

        if (m_civilian == null)
            return;

        bool hasLoS = m_civilian.HasLoS();

        // Usamos señales múltiples para determinar si hay camino viable.
        bool hasPath = m_civilian.HasFleePath;
        float remainingDistance = m_civilian.GetRemainingFleeDistance();

        // Si no tenemos path aún, intentar estimar con la distancia directa al nodo objetivo.
        float estimatedSafeDist = m_civilian.GetEstimatedDistanceToSafeNode();

        if (!float.IsInfinity(remainingDistance) && remainingDistance > 0f)
        {
            hasPath = true;
        }
        else if (!float.IsInfinity(estimatedSafeDist))
        {
            hasPath = true;
            remainingDistance = estimatedSafeDist;
        }

        float reference = Mathf.Max(1f, m_civilian.SafeDistance * 2f);
        float far01 = hasPath
            ? Mathf.Clamp01((remainingDistance - m_civilian.SafeDistance) / reference) // 0 = cerca, 1 = lejos
            : 1f; // sin path: tratar como “no hay salida clara”

        if (!hasPath)
        {
            attackWeight = 3f;
            escapeWeight = 0.35f;
            LogDT($"[Roulette] No path/Infinity: atk {attackWeight:F2}, esc {escapeWeight:F2}");
            return;
        }

        // Más lejos del nodo seguro => subir ataque y bajar escape.
        // Más cerca del nodo seguro => bajar ataque y subir escape.
        float escapeBoost = Mathf.Lerp(3.5f, 0.35f, far01);
        float attackBoost = Mathf.Lerp(0.5f, 3.5f, far01);

        attackWeight *= attackBoost;
        escapeWeight *= escapeBoost;

        // Decaimiento gradual si estamos sin LoS: reduce intención de ataque y sube escape con el tiempo
        if (!hasLoS)
        {
            float t = Mathf.Clamp01(m_timeWithoutLoS / 3f); // en 3s sin LoS llega al máximo decaimiento
            float attackDecay = Mathf.Lerp(1f, 0.3f, t);
            float escapeGrowth = Mathf.Lerp(1f, 2.5f, t);

            attackWeight *= attackDecay;
            escapeWeight *= escapeGrowth;

            LogDT($"[Roulette] no LoS decay t={t:F2}, attackDecay={attackDecay:F2}, escapeGrowth={escapeGrowth:F2}, timeNoLoS={m_timeWithoutLoS:F2}s");
        }

        LogDT($"[Roulette] remDist={remainingDistance:F1}, far01={far01:F2}, hasLoS={hasLoS}, atk={attackWeight:F2} (x{attackBoost:F2}), esc={escapeWeight:F2} (x{escapeBoost:F2})");
    }

    /// <summary>
    /// Actualiza el timer de tiempo sin LoS para aplicar decaimiento gradual de intención.
    /// </summary>
    private void UpdateLoSTimer(bool hasLoS)
    {
        if (m_civilian == null) return;

        if (hasLoS)
        {
            m_timeWithoutLoS = 0f;
        }
        else
        {
            m_timeWithoutLoS += evaluationInterval;
        }
    }

    /// <summary>
    /// Helper to log DT messages both via MyLogger and MyLogger.LogInfo when debugDT is enabled.
    /// </summary>
    private void LogDT(string message)
    {
        if (!debugDT) return;
        MyLogger.LogInfo(message);
        MyLogger.LogInfo(message);
    }

    /// <summary>
    /// Lock the civilian into a specific stance to prevent flip-flop
    /// </summary>
    private void LockStance(CivilianStance p_stance, string p_reason)
    {
        m_currentStance = p_stance;
        m_stanceLockUntil = Time.time + m_stanceLockDuration;
        m_hasActiveStanceLock = true;
        m_cachedEvaluationStance = p_stance;
        m_hasCachedStanceEvaluation = true;
        m_civilian.SetDyingEvadeMode(p_stance == CivilianStance.Dying);

        if (debugDT)
            MyLogger.LogInfo($"STANCE LOCKED: {p_stance} for {m_stanceLockDuration}s - Reason: {p_reason}");
    }

    /// <summary>
    /// Break stance lock due to specific triggers (distance, LoS loss, etc.)
    /// </summary>
    private void BreakStanceLock(string p_reason)
    {
        if (m_hasActiveStanceLock)
        {
            if (debugDT)
                MyLogger.LogInfo($"STANCE LOCK BROKEN: {p_reason}");
            m_hasActiveStanceLock = false;
            m_stanceLockUntil = 0f;
            m_hasCachedStanceEvaluation = false;
            m_civilian.SetDyingEvadeMode(false);
        }
    }

    /// <summary>
    /// Check if stance lock should be broken due to context changes
    /// </summary>
    private void CheckStanceLockBreakers()
    {
        if (!m_hasActiveStanceLock) return;

        bool isDyingStance = m_currentStance == CivilianStance.Dying;

        // Null check for player safety
        if (m_civilian?.Player == null) 
        {
            BreakStanceLock("Player reference lost");
            return;
        }

        if (isDyingStance)
        {
            // Dying stance is meant to persist even if player gets far;
            // only explicit state changes or lock expiration should break it.
            return;
        }

        float l_distanceToPlayer = Vector3.Distance(m_civilian.transform.position, m_civilian.Player.position);
        // Solo romper por distancia en ATTACK; en ESCAPE no cortamos la huida solo por alejamiento.
        if (m_currentStance == CivilianStance.Attack && l_distanceToPlayer >= m_civilian.SafeDistance)
        {
            BreakStanceLock($"Player too far ({l_distanceToPlayer:F1} >= {m_civilian.SafeDistance})");
            return;
        }

        // Si estamos en Attack pero ya llegamos al área del safe node, soltar lock para permitir huida
        if (m_currentStance == CivilianStance.Attack)
        {
            float remDist = m_civilian.GetRemainingFleeDistance();
            if (!float.IsInfinity(remDist) && remDist <= m_civilian.SafeDistance)
            {
                BreakStanceLock($"Reached safe node area (remaining {remDist:F1} <= {m_civilian.SafeDistance})");
                return;
            }
        }

        // Break ATTACK stance lock if lost LoS for too long (using VISIBLE timer)
        if (m_currentStance == CivilianStance.Attack && !m_civilian.HasLoS())
        {
            m_pursuitLoseSightTimerVisible += evaluationInterval;
            // Lejos del nodo seguro: permitir mayor gracia antes de soltar Attack
            float remainingDist = m_civilian.GetRemainingFleeDistance();
            float graceMultiplier = (remainingDist > m_civilian.SafeDistance * 2f) ? 6f : 2f;
            float graceThreshold = m_civilian.AttackLoseSightGrace * graceMultiplier;

            if (m_pursuitLoseSightTimerVisible >= graceThreshold)
            {
                BreakStanceLock($"Lost LoS too long in ATTACK stance ({m_pursuitLoseSightTimerVisible:F2}s >= {graceThreshold:F2}s)");
                return;
            }
        }
        else if (m_civilian.HasLoS())
        {
            m_pursuitLoseSightTimerVisible = 0f; // Reset visible timer if we can see player
        }

        float combined = m_civilian.AttackWeight + m_civilian.EscapeWeight;
        if (m_civilian.DyingWeight > combined && m_civilian.DyingWeight > 0f)
        {
            BreakStanceLock("Dying weight dominance");
            return;
        }

        // Break lock if attack cycle completed (post-hit scenarios handled separately)
        if (m_isInAttackCycle)
        {
            float l_totalAttackDuration = m_civilian.AttackWindup + m_civilian.AttackHitWin + m_civilian.AttackRecover;
            float l_timeSinceAttackStart = Time.time - m_attackCycleStartTime;
            
            if (l_timeSinceAttackStart >= l_totalAttackDuration)
            {
                EndAttackCycle(true); // End with post-hit flee
                return; // EndAttackCycle will break the stance lock
            }
        }
    }

    /// <summary>
    /// Determine if an alert should be triggered with cooldown protection
    /// </summary>
    private bool ShouldTriggerAlert()
    {
        // Check if enough time has passed since last alert
        if (Time.time - m_lastAlertTime < alertCooldown)
        {
            return false; // Still in cooldown period
        }

        // Check random chance
        bool l_shouldAlert = UnityEngine.Random.value < alertChanceWhenNoLoS;

        if (l_shouldAlert)
        {
            m_lastAlertTime = Time.time; // Update last alert time
        }

        return l_shouldAlert;
    }

    /// <summary>
    /// Determine if civilian should return to idle based on safety conditions and timing
    /// </summary>
    private bool ShouldReturnToIdle()
    {
        if (debugDT)
            MyLogger.LogInfo("=== ShouldReturnToIdle() START ===");

        // CivilianDecisionTreeRunner.cs  (dentro de ShouldReturnToIdle, al inicio)
        if (IsCurrentlyFleeing() && m_civilian != null)
        {
            // Si usamos grafo y todavía no llegamos al nodo seguro, seguir huyendo
            // (evita cortar por SafeDistance)
            if (m_civilian.HasFleeGraph && m_civilian.HasFleePath && !m_civilian.FleePathReachedEnd())
            {
                // resetear cualquier safe timer local de DT, seguimos huyendo
                m_safeTimer = 0f;
                if (debugDT)
                    MyLogger.LogInfo("FLEE A*: aún no llegué al nodo seguro → continuar huyendo (ignorar SafeDistance)");
                return false;
            }
        }

        if (m_civilian == null || m_civilian.Player == null)
        {
            if (debugDT)
                MyLogger.LogInfo("CIVILIAN OR PLAYER IS NULL - RETURNING TRUE");
            return true;
        }

        float l_distanceToPlayer = Vector3.Distance(m_civilian.transform.position, m_civilian.Player.position);
        bool l_isSafeDistance = l_distanceToPlayer >= m_civilian.SafeDistance;
        bool l_hasLoS = m_civilian.HasLoS();

        if (debugDT)
            MyLogger.LogInfo($"Distance: {l_distanceToPlayer:F1}, SafeDistance: {m_civilian.SafeDistance}, HasLoS: {l_hasLoS}");

        // SafeDistance is only for STOPPING fleeing, not for starting it
        // If player is in safe area (beyond SafeDistance), return to idle immediately
        if (l_isSafeDistance)
        {
            if (debugDT)
                MyLogger.LogInfo("PLAYER IN SAFE AREA - STOP FLEEING");
            m_safeTimer = 0f;
            return true;
        }

        // If still close but no LoS, use grace timer (for when we lost sight during chase)
        if (!l_hasLoS)
        {
            m_safeTimer += evaluationInterval;
            bool l_shouldReturn = m_safeTimer >= m_civilian.SafeTime;

            if (debugDT)
                MyLogger.LogInfo($"CLOSE BUT NO LoS - GRACE TIMER: Timer={m_safeTimer:F2}/{m_civilian.SafeTime}, ShouldReturn={l_shouldReturn}");
            return l_shouldReturn;
        }

        // Player is close and visible - keep fleeing
        if (debugDT)
            MyLogger.LogInfo("PLAYER CLOSE AND VISIBLE - CONTINUE FLEEING");
        m_safeTimer = 0f;
        return false;
    }

    /// <summary>
    /// Check if civilian is currently in a fleeing state
    /// </summary>
    private bool IsCurrentlyFleeing()
    {
        string l_currentState = GetCurrentFsmStateName();
        return l_currentState == "S_CivFlee" || l_currentState == "S_CivEvade";
    }

    /// <summary>
    /// Check if civilian is currently pursuing/attacking
    /// </summary>
    private bool IsCurrentlyPursuing()
    {
        string l_currentState = GetCurrentFsmStateName();
        return l_currentState == "S_CivPersuit" || l_currentState == "S_CivAttack";
    }

    /// <summary>
    /// Check if civilian should stay committed to pursuit despite losing LoS
    /// </summary>
    private bool IsWithinPursuitCommitment()
    {
        // Give more commitment time than the lose sight grace (attackLoseSightGrace is 0.3s)
        float l_commitmentTime = m_civilian.AttackLoseSightGrace * 3f; // 0.9s commitment
        float l_timeSincePursuitStart = Time.time - m_pursuitStartTime;

        // Always commit for at least 1 second after starting pursuit
        if (l_timeSincePursuitStart < 1f)
        {
            if (debugDT)
                MyLogger.LogInfo($"PURSUIT COMMITMENT: Within minimum commitment time ({l_timeSincePursuitStart:F2}s < 1.0s)");
            return true;
        }

        // Then use INVISIBLE lose sight timer with extended grace (separate from visible timer)
        if (!m_civilian.HasLoS())
        {
            m_pursuitLoseSightTimerInvisible += evaluationInterval;
        }
        else
        {
            m_pursuitLoseSightTimerInvisible = 0f; // Reset invisible timer when LoS recovered
        }

        bool l_stillCommitted = m_pursuitLoseSightTimerInvisible < l_commitmentTime;

        if (debugDT)
            MyLogger.LogInfo($"PURSUIT COMMITMENT: Invisible LoS timer={m_pursuitLoseSightTimerInvisible:F2}s < {l_commitmentTime:F2}s = {l_stillCommitted}");
        return l_stillCommitted;
    }

    /// <summary>
    /// Check if evade time has elapsed and should transition to flee
    /// </summary>
    private bool ShouldTransitionFromEvade()
    {
        return (Time.time - m_evadeStartTime) >= m_civilian.EvadeTime;
    }


    #endregion

    #region Action Suggestions

    /// <summary>
    /// Suggest fleeing from the player
    /// </summary>
    private void SuggestFlee()
    {
        m_civilian.SetDyingEvadeMode(false);
        SetSuggestion("Flee");

        if (debugDT)
        {
            // Determine the reason for fleeing based on current context
            bool l_hasLoS = m_civilian.HasLoS();
            string l_reason = l_hasLoS ? "Player visible, roulette chose escape" : "No LoS, continuing flee behavior";
            MyLogger.LogInfo($"DT → Flee ({l_reason})");
        }
    }

    /// <summary>
    /// Suggest attacking the player (dedicated attack state)
    /// </summary>
    private void SuggestAttack()
    {
        m_civilian.SetDyingEvadeMode(false);
        // Start attack cycle if not already in one
        if (!m_isInAttackCycle)
        {
            StartAttackCycle();
            
            if (debugDT)
                MyLogger.LogInfo($"DT → Attack (Starting attack cycle in melee range)");
        }
        else if (debugDT)
        {
            float l_totalAttackDuration = m_civilian.AttackWindup + m_civilian.AttackHitWin + m_civilian.AttackRecover;
            float l_timeSinceAttackStart = Time.time - m_attackCycleStartTime;
            MyLogger.LogInfo($"DT → Attack (Continuing attack cycle: {l_timeSinceAttackStart:F2}s / {l_totalAttackDuration:F2}s)");
        }

        SetSuggestion("Attack");
    }

    /// <summary>
    /// Suggest pursuing/attacking the player
    /// </summary>
    private void SuggestPursue()
    {
        m_civilian.SetDyingEvadeMode(false);
        // Initialize pursuit timing if entering pursuit for first time
        if (m_currentSuggestion != "Pursue")
        {
            m_pursuitStartTime = Time.time;
            m_pursuitLoseSightTimerInvisible = 0f; // Reset invisible lose sight timer when starting pursuit

            if (debugDT)
                MyLogger.LogInfo($"DT → Pursue (Starting pursuit - commitment time initialized)");
        }
        else
        {
            // Reset invisible lose sight timer if we can see player
            if (m_civilian.HasLoS())
            {
                m_pursuitLoseSightTimerInvisible = 0f;
            }
        }

        SetSuggestion("Pursue");

        if (debugDT && m_currentSuggestion == "Pursue")
            MyLogger.LogInfo($"DT → Pursue (Continuing pursuit)");
    }

    /// <summary>
    /// Suggest evading the player (short burst movement)
    /// </summary>
    
private void SuggestEvade()
    {
        m_civilian.SetDyingEvadeMode(false);
        // If we're already evading, check if time elapsed
        if (m_currentSuggestion == "Evade" && ShouldTransitionFromEvade())
        {
            SuggestFlee();
            return;
        }

        // Set evade start time if entering evade
        if (m_currentSuggestion != "Evade")
        {
            m_evadeStartTime = Time.time;
        }

        SetSuggestion("Evade");

        if (debugDT)
            MyLogger.LogInfo($"DT ? Evade (Player visible, evading for {m_civilian.EvadeTime}s)");
    }

    /// <summary>
    /// Suggest the permanent dying/limp evade
    /// </summary>
    private void SuggestDying()
    {
        m_civilian.SetDyingEvadeMode(true);
        SetSuggestion("Dying");

        if (debugDT)
            MyLogger.LogInfo($"DT -> Dying (Health loss {m_civilian.HealthLostNormalized:P0}, Weight={m_civilian.DyingWeight:F2})");
    }

    /// <summary>
    /// Suggest idle behavior
/// <summary>
    /// Suggest idle behavior
    /// </summary>
    private void SuggestIdle()
    {
        m_civilian.SetDyingEvadeMode(false);
        SetSuggestion("Idle");

        if (debugDT)
            MyLogger.LogInfo($"DT → Idle (Safe conditions met)");
    }

    /// <summary>
    /// Suggest alerting other NPCs and then resuming
    /// </summary>
    private void SuggestAlert()
    {
        m_civilian.SetDyingEvadeMode(false);
        // Trigger global alert only if it's not already set
        var l_currentAlert = BlackboardService.GetValue<bool>(BlackboardKeys.GLOBAL_ALERT);
        if (!l_currentAlert)
        {
            BlackboardService.SetValue(BlackboardKeys.GLOBAL_ALERT, true);
            
            if (debugDT)
                MyLogger.LogInfo($"DT → Alert (No LoS, setting GLOBAL_ALERT to true, cooldown: {alertCooldown}s)");
        }
        else if (debugDT)
        {
            MyLogger.LogInfo($"DT → Alert (No LoS, GLOBAL_ALERT already true, skipping)");
        }
    
        SetSuggestion(resumeSuggestion);
    }

    /// <summary>
    /// Suggest resuming normal behavior
    /// </summary>
    private void SuggestResume()
    {
        m_civilian.SetDyingEvadeMode(false);
        SetSuggestion(resumeSuggestion);
        
        if (debugDT)
            MyLogger.LogInfo($"DT → Resume (No LoS, continuing normal behavior)");
    }

    /// <summary>
    /// Set the current suggestion and track changes
    /// </summary>
    private void SetSuggestion(string p_suggestion)
    {
        if (m_currentSuggestion != p_suggestion)
        {
            m_lastSuggestion = m_currentSuggestion;
            m_currentSuggestion = p_suggestion;
        }
    }

    #endregion

    #region Evaluation Loop

    /// <summary>
    /// Start the decision tree evaluation loop
    /// </summary>
    private void StartEvaluationLoop()
    {
        if (m_evaluationCoroutine == null)
        {
            m_evaluationCoroutine = StartCoroutine(EvaluationLoop());
        }
    }

    /// <summary>
    /// Stop the decision tree evaluation loop
    /// </summary>
    private void StopEvaluationLoop()
    {
        if (m_evaluationCoroutine != null)
        {
            StopCoroutine(m_evaluationCoroutine);
            m_evaluationCoroutine = null;
        }
    }

    /// <summary>
    /// Main evaluation loop that runs the decision tree at intervals
    /// </summary>
    private IEnumerator EvaluationLoop()
    {
        var l_waitTime = new WaitForSeconds(evaluationInterval);

        while (enabled && gameObject.activeInHierarchy)
        {
            yield return l_waitTime;

            // Skip evaluation if civilian is not alive
            if (!m_civilian.IsActive)
                continue;

            // Check if stance lock should be broken due to context changes
            CheckStanceLockBreakers();

            // Run the decision tree
            string l_previousSuggestion = m_currentSuggestion;
            
            if (m_root != null)
            {
                m_hasCachedStanceEvaluation = false;
                m_root.Execute();
            }

            // Process suggestion if it changed OR if FSM is not in the suggested state
            bool l_suggestionChanged = m_currentSuggestion != l_previousSuggestion;
            bool l_needsStateSync = !string.IsNullOrEmpty(m_currentSuggestion) && !IsCurrentFsmStateMatchingSuggestion();
            
            if ((l_suggestionChanged || l_needsStateSync) && !string.IsNullOrEmpty(m_currentSuggestion))
            {
                if (debugDT && l_needsStateSync && !l_suggestionChanged)
                    MyLogger.LogInfo($"DT re-processing suggestion '{m_currentSuggestion}' (FSM state sync needed)");
                    
                ProcessSuggestion(m_currentSuggestion);
            }

            m_lastEvaluationTime = Time.time;
        }
    }

    #endregion

    #region FSM State Checking

    /// <summary>
    /// Check if the current FSM state matches the current DT suggestion
    /// </summary>
    private bool IsCurrentFsmStateMatchingSuggestion()
    {
        if (m_civilian == null || string.IsNullOrEmpty(m_currentSuggestion))
            return true; // Assume match if we can't determine

        // Map the suggestion to the expected FSM state name
        string l_expectedStateName = MapSuggestionToFsmStateName(m_currentSuggestion);
        
        // Get current FSM state name
        string l_currentStateName = GetCurrentFsmStateName();
        
        if (debugDT && !string.IsNullOrEmpty(l_currentStateName))
        {
            // Only log occasionally to avoid spam, or when there's a mismatch
            bool l_isMatch = string.Equals(l_currentStateName, l_expectedStateName, System.StringComparison.OrdinalIgnoreCase);
            if (!l_isMatch)
            {
                MyLogger.LogInfo($"FSM state mismatch - Current: '{l_currentStateName}', Expected: '{l_expectedStateName}'");
            }
        }

        return string.Equals(l_currentStateName, l_expectedStateName, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Map DT suggestion to FSM state name (same logic as in Civilian)
    /// </summary>
    private string MapSuggestionToFsmStateName(string p_suggestion)
    {
        switch (p_suggestion.ToLower())
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
                return "S_CivEvade";
                
            default:
                return p_suggestion;
        }
    }

    /// <summary>
    /// Get the current FSM state name
    /// </summary>
    private string GetCurrentFsmStateName()
    {
        // Use reflection to access the civilian's FSM state
        if (m_civilian != null)
        {
            try
            {
                var l_civilianType = m_civilian.GetType();
                var l_stateMachineField = l_civilianType.GetField("stateMachine", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (l_stateMachineField?.GetValue(m_civilian) is Scripts.FSM.Base.StateMachine.StateMachine l_stateMachine)
                {
                    var l_currentState = l_stateMachine.GetCurrentState();
                    return l_currentState?.State?.StateName ?? "";
                }
            }
            catch (System.Exception l_e)
            {
                if (debugDT)
                    MyLogger.LogWarning($"Failed to get FSM state name: {l_e.Message}");
            }
        }
        
        return "";
    }

    #endregion

    #region FSM Integration

    /// <summary>
    /// Process a suggestion from the decision tree by requesting FSM state changes
    /// </summary>
    private void ProcessSuggestion(string p_suggestion)
    {
        // Skip processing if we're in a non-interruptible attack cycle
        if (IsInNonInterruptibleAttackCycle() && p_suggestion != "Attack")
        {
            if (debugDT)
                MyLogger.LogInfo($"Ignoring suggestion '{p_suggestion}' - in non-interruptible attack cycle");
            return;
        }

        // Note: The Civilian FSM should handle the actual state transitions
        // This is just a bridge to communicate the decision tree's suggestion

        switch (p_suggestion)
        {
            case "Attack":
                RequestStateChange("Attack");
                break;

            case "Flee":
                RequestStateChange("Fleeing");
                break;

            case "Pursue":
                RequestStateChange("Pursuing");
                break;

            case "Evade":
                RequestStateChange("Evading");
                break;

            case "Dying":
                RequestStateChange("Evading");
                break;

            case "Idle":
                RequestStateChange("Idle");
                break;

            default:
                RequestStateChange(p_suggestion);
                break;
        }
    }

    /// <summary>
    /// Request a state change from the Civilian's FSM
    /// </summary>
    private void RequestStateChange(string p_stateName)
    {
        // Use the Civilian's RequestStateChange method to bridge to FSM
        if (m_civilian != null)
        {
            m_civilian.RequestStateChange(p_stateName);
        }
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Called by Civilian FSM when an attack cycle completes
    /// </summary>
    public void OnAttackCycleComplete()
    {
        if (m_isInAttackCycle)
        {
            EndAttackCycle(true); // End with post-hit flee
            
            if (debugDT)
                MyLogger.LogInfo($"Attack cycle completed - starting post-hit flee phase");
        }
    }

    /// <summary>
    /// Called by Civilian FSM when melee damage is dealt
    /// </summary>
    public void OnMeleeDamageDealt()
    {
        if (debugDT)
            MyLogger.LogInfo($"Melee damage dealt - attack cycle will complete soon");
        
        // The attack cycle will complete naturally and trigger post-hit flee
    }

    /// <summary>
    /// Manually trigger a decision tree evaluation (useful for testing)
    /// </summary>
    [ContextMenu("Evaluate Decision Tree")]
    public void EvaluateDecisionTree()
    {
        if (m_root != null)
        {
            string l_previousSuggestion = m_currentSuggestion;
            m_hasCachedStanceEvaluation = false;
            m_root.Execute();
            
            if (debugDT)
                MyLogger.LogInfo($"Manual DT evaluation: {m_currentSuggestion}");
                
            if (m_currentSuggestion != l_previousSuggestion)
            {
                ProcessSuggestion(m_currentSuggestion);
            }
        }
    }

    /// <summary>
    /// Get current decision tree status for debugging
    /// </summary>
    public string GetStatus()
    {
        return $"Current: {m_currentSuggestion}, Last: {m_lastSuggestion}, LastEval: {Time.time - m_lastEvaluationTime:F2}s ago";
    }

    #endregion

    #region Debug

    [ContextMenu("Debug Decision Tree Status")]
    private void DebugDecisionTreeStatus()
    {
        MyLogger.LogInfo("=== CIVILIAN DECISION TREE STATUS ===");
        MyLogger.LogInfo($"Evaluation Interval: {evaluationInterval}s");
        MyLogger.LogInfo($"Alert Chance (No LoS): {alertChanceWhenNoLoS:P0}");
        MyLogger.LogInfo($"Alert Cooldown: {alertCooldown}s");
        MyLogger.LogInfo($"Post-Hit Flee Time: {postHitFleeTime}s");
        MyLogger.LogInfo($"Time Since Last Alert: {(m_lastAlertTime < 0 ? "Never" : (Time.time - m_lastAlertTime).ToString("F2") + "s")}");
        MyLogger.LogInfo($"Resume Suggestion: {resumeSuggestion}");
        MyLogger.LogInfo($"Current Suggestion: {m_currentSuggestion}");
        MyLogger.LogInfo($"Last Suggestion: {m_lastSuggestion}");
        MyLogger.LogInfo($"Current FSM State: {GetCurrentFsmStateName()}");
        MyLogger.LogInfo($"Expected FSM State: {MapSuggestionToFsmStateName(m_currentSuggestion)}");
        MyLogger.LogInfo($"FSM State Matches: {IsCurrentFsmStateMatchingSuggestion()}");
        MyLogger.LogInfo($"Last Evaluation: {Time.time - m_lastEvaluationTime:F2}s ago");
        
        // Stance Lock Status
        MyLogger.LogInfo($"--- STANCE LOCK ---");
        MyLogger.LogInfo($"Has Active Lock: {m_hasActiveStanceLock}");
        MyLogger.LogInfo($"Current Stance: {m_currentStance}");
        MyLogger.LogInfo($"Lock Expires In: {(m_hasActiveStanceLock ? (m_stanceLockUntil - Time.time).ToString("F2") + "s" : "N/A")}");
        
        // Attack Cycle Status
        MyLogger.LogInfo($"--- ATTACK CYCLE ---");
        MyLogger.LogInfo($"Is In Attack Cycle: {m_isInAttackCycle}");
        MyLogger.LogInfo($"Attack Start Time: {(m_isInAttackCycle ? (Time.time - m_attackCycleStartTime).ToString("F2") + "s ago" : "N/A")}");
        MyLogger.LogInfo($"Post-Hit Flee Active: {m_postHitFleeActive}");
        MyLogger.LogInfo($"Post-Hit Flee Time Left: {(m_postHitFleeActive ? (postHitFleeTime - (Time.time - m_postHitFleeStartTime)).ToString("F2") + "s" : "N/A")}");
        
        // Timers Status
        MyLogger.LogInfo($"--- TIMERS ---");
        MyLogger.LogInfo($"Pursuit LoS Timer (Visible): {m_pursuitLoseSightTimerVisible:F2}s");
        MyLogger.LogInfo($"Pursuit LoS Timer (Invisible): {m_pursuitLoseSightTimerInvisible:F2}s");
        MyLogger.LogInfo($"Safe Timer: {m_safeTimer:F2}s");
        MyLogger.LogInfo($"Pursuit Start Time: {(m_pursuitStartTime > 0 ? (Time.time - m_pursuitStartTime).ToString("F2") + "s ago" : "Never")}");
        
        // General Status
        MyLogger.LogInfo($"--- GENERAL ---");
        MyLogger.LogInfo($"Debug Enabled: {debugDT}");
        MyLogger.LogInfo($"Can See Player: {(m_civilian != null ? m_civilian.HasLoS() : "N/A")}");
        MyLogger.LogInfo($"Can Attack: {(m_civilian != null ? m_civilian.CanAttack : "N/A")}");
        MyLogger.LogInfo($"In Melee Range: {(m_civilian != null ? m_civilian.IsPlayerInMeleeRange() : "N/A")}");
        MyLogger.LogInfo($"Distance to Player: {(m_civilian != null && m_civilian.Player != null ? Vector3.Distance(m_civilian.transform.position, m_civilian.Player.position).ToString("F2") : "N/A")}");
        
        if (BlackboardService != null)
        {
            MyLogger.LogInfo($"Global Alert: {BlackboardService.GetValue<bool>(BlackboardKeys.GLOBAL_ALERT)}");
        }
        
        MyLogger.LogInfo("====================================");
    }

    #endregion
}
