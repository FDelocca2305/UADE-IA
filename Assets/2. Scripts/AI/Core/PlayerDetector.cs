using System.Collections;
using Services.MicroServices.BlackboardService;
using Services;
using UnityEngine;

/// <summary>
/// Advanced player detection system implementing realistic Line of Sight.
/// Integrates with the blackboard system for AI coordination.
///
/// IMPROVEMENT: Intelligent caching system for performance
/// IMPROVEMENT: Gradual detection with alert levels
/// IMPROVEMENT: Complete debug visualization with gizmos
/// IMPROVEMENT: Support for multiple detection types (visual, auditory, etc.) NEED CHECK SLuser
/// </summary>
public class PlayerDetector : MonoBehaviour, IPlayerDetector
{
    [Header("Detection Configuration")]
    [SerializeField] private DetectionConfig config = DetectionConfig.GetDefault(AIPersonalityType.Conservative);

    
    [Header("Personality Integration")]
    [SerializeField] private AIPersonalityType personalityType = AIPersonalityType.Conservative;
    [SerializeField] private bool autoConfigureFromPersonality = true;
    
    [Header("Performance Optimization")]
    [SerializeField] private bool useFrameCaching = true;
    [SerializeField] private int maxCacheFrames = 3;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool enableDetectionLogs = false;
    [SerializeField] private bool showDetectionInfo = true;
    
    // Core detection state
    private DetectionResult m_lastDetectionResult = DetectionResult.None;
    private Transform m_cachedPlayerTransform;
    
    // Performance caching
    private int m_lastUpdateFrame = -1;
    private DetectionResult m_cachedResult = DetectionResult.None;
    
    // Runtime tracking
    private Vector3 m_lastKnownPlayerPosition = Vector3.zero;
    private float m_lastSeenTime = -1f;
    private PlayerDetectionLevel m_previousDetectionLevel = PlayerDetectionLevel.None;
    
    // Coroutine management
    private Coroutine m_detectionCoroutine;
    
    // Debug info
    private string m_lastBlockingObject = "";
    private Color m_currentGizmoColor = Color.yellow;
    
    private static IBlackboardService BlackboardService => ServiceLocator.Get<IBlackboardService>();
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (autoConfigureFromPersonality)
        {
            // Preserve designer overrides (layer masks, tag) when reapplying defaults
            var preservedObstacleMask = config.obstacleLayerMask;
            var preservedPlayerMask = config.playerLayerMask;
            var preservedPlayerTag = config.playerTag;

            config = DetectionConfig.GetDefault(personalityType);
            config.obstacleLayerMask = preservedObstacleMask;
            config.playerLayerMask = preservedPlayerMask;
            if (!string.IsNullOrEmpty(preservedPlayerTag))
            {
                config.playerTag = preservedPlayerTag;
            }
        }
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void OnEnable()
    {
        StartDetectionCoroutine();
    }
    
    private void OnDisable()
    {
        StopDetectionCoroutine();
    }
    
    private void OnDestroy()
    {
        StopDetectionCoroutine();
    }
    
    #endregion
    
    #region Initialization
    
    private void Initialize()
    {
        // Find player reference
        FindPlayerReference();
        
        // Apply personality-based config if needed
        if (autoConfigureFromPersonality)
        {
            ApplyPersonalityConfig();
        }
        
        if (enableDetectionLogs)
            MyLogger.LogInfo($"PlayerDetector on {gameObject.name}: Initialized with personality {personalityType}");
    }
    
    private void FindPlayerReference()
    {
        // First try to get from blackboard
        m_cachedPlayerTransform = BlackboardService.GetValue<Transform>(BlackboardKeys.PLAYER_TRANSFORM);
        
        // Fallback to finding by tag
        if (m_cachedPlayerTransform) 
            return;
        
        var l_playerObject = GameObject.FindGameObjectWithTag(config.playerTag);
        if (l_playerObject)
        {
            m_cachedPlayerTransform = l_playerObject.transform;
                
            // Update blackboard with found player
            BlackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, m_cachedPlayerTransform);
                
            if (enableDetectionLogs)
                MyLogger.LogInfo($"PlayerDetector: Found player by tag '{config.playerTag}'");
        }
        else
        {
            MyLogger.LogWarning($"PlayerDetector on {gameObject.name}: Player not found with tag '{config.playerTag}'");
        }
    }
    
    private void ApplyPersonalityConfig()
    {
        // Configuración automática basada en personalidad
        // Esto permite balancing centralizado sin modificar cada prefab
        var l_newConfig = DetectionConfig.GetDefault(personalityType);
        
        // Preserve any manual overrides that make sense
        l_newConfig.obstacleLayerMask = config.obstacleLayerMask;
        l_newConfig.playerLayerMask = config.playerLayerMask;
        l_newConfig.playerTag = config.playerTag;
        
        config = l_newConfig;
        
        if (enableDetectionLogs)
            MyLogger.LogInfo($"PlayerDetector: Applied {personalityType} personality config");
    }
    
    #endregion
    
    #region Detection Coroutine Management
    
    private void StartDetectionCoroutine()
    {
        if (m_detectionCoroutine == null)
        {
            m_detectionCoroutine = StartCoroutine(DetectionUpdateCoroutine());
        }
    }
    
    private void StopDetectionCoroutine()
    {
        if (m_detectionCoroutine != null)
        {
            StopCoroutine(m_detectionCoroutine);
            m_detectionCoroutine = null;
        }
    }
    
    private IEnumerator DetectionUpdateCoroutine()
    {
        while (enabled && gameObject.activeInHierarchy)
        {
            UpdateDetection();
            yield return new WaitForSeconds(config.updateRate);
        }
    }
    
    #endregion
    
    #region Core Detection Logic
    
    private void UpdateDetection()
    {
        if (!m_cachedPlayerTransform)
        {
            FindPlayerReference();
            return;
        }
        
        // Perform detection
        var l_currentResult = PerformDetection(m_cachedPlayerTransform);
        
        // Check for level changes
        if (l_currentResult.level != m_previousDetectionLevel)
        {
            OnDetectionLevelChanged(m_previousDetectionLevel, l_currentResult.level);
            m_previousDetectionLevel = l_currentResult.level;
        }
        
        // Update blackboard if significant detection
        if (l_currentResult.IsSignificant)
        {
            UpdateBlackboardWithDetection(l_currentResult);
        }
        
        // Store result
        m_lastDetectionResult = l_currentResult;
        
        // Update gizmo color for visualization
        UpdateGizmoColor(l_currentResult.level);
    }
    
    private DetectionResult PerformDetection(Transform p_player)
    {
        // Frame caching para performance
        if (useFrameCaching && m_lastUpdateFrame == Time.frameCount)
        {
            return m_cachedResult;
        }
        
        if (!p_player)
        {
            return DetectionResult.None;
        }
        
        // Get positions
        Vector3 l_eyePosition = GetEyePosition();
        Vector3 l_playerPosition = p_player.position;
        
        // Calculate basic metrics
        float l_distance = Vector3.Distance(l_eyePosition, l_playerPosition);
        Vector3 l_directionToPlayer = (l_playerPosition - l_eyePosition).normalized;
        float l_angle = Vector3.Angle(transform.forward, l_directionToPlayer);
        
        // Check distance first (early exit)
        if (l_distance > config.detectionRange)
        {
            var l_result = new DetectionResult(
                PlayerDetectionLevel.None, false, false, false,
                l_distance, l_angle, m_lastKnownPlayerPosition, GetTimeSinceLastSeen(), ""
            );
            
            CacheResult(l_result);
            return l_result;
        }
        
        // Check field of view
        bool l_inMainFOV = IsInFieldOfView(l_angle, config.fieldOfView);
        bool l_inPeripheralFOV = config.usePeripheralVision && IsInFieldOfView(l_angle, 120f);
        
        if (!l_inMainFOV && !l_inPeripheralFOV)
        {
            var l_result = new DetectionResult(
                PlayerDetectionLevel.None, false, false, false,
                l_distance, l_angle, m_lastKnownPlayerPosition, GetTimeSinceLastSeen(), ""
            );
            
            CacheResult(l_result);
            return l_result;
        }
        
        // Check line of sight
        bool l_hasLineOfSight = CheckLineOfSight(l_eyePosition, l_playerPosition, out string l_blocker);
        
        // Determine detection level
        PlayerDetectionLevel l_level = CalculateDetectionLevel(l_distance, l_angle, l_inMainFOV, l_inPeripheralFOV, l_hasLineOfSight);
        
        // Update tracking info if visible
        if (l_hasLineOfSight)
        {
            m_lastKnownPlayerPosition = l_playerPosition;
            m_lastSeenTime = Time.time;
        }
        
        var l_finalResult = new DetectionResult(
            l_level, l_hasLineOfSight, l_inMainFOV || l_inPeripheralFOV, l_hasLineOfSight,
            l_distance, l_angle, m_lastKnownPlayerPosition, GetTimeSinceLastSeen(), l_blocker
        );
        
        CacheResult(l_finalResult);
        return l_finalResult;
    }
    
    private PlayerDetectionLevel CalculateDetectionLevel(float p_distance, float p_angle, bool p_inMainFOV, bool p_inPeripheralFOV, bool p_hasLineOfSight)
    {
        if (!p_hasLineOfSight)
        {
            return PlayerDetectionLevel.None;
        }
        
        // Sistema gradual de detección para comportamientos más naturales
        float l_maxRange = config.detectionRange;
        float l_immediateRange = l_maxRange * 0.25f;
        float l_clearRange = l_maxRange * 0.5f;
        float l_partialRange = l_maxRange * 0.75f;
        
        if (p_distance <= l_immediateRange && p_inMainFOV)
        {
            return PlayerDetectionLevel.Immediate;
        }
        else if (p_distance <= l_clearRange && p_inMainFOV)
        {
            return PlayerDetectionLevel.Clear;
        }
        else if (p_distance <= l_partialRange && p_inMainFOV)
        {
            return PlayerDetectionLevel.Partial;
        }
        else if (p_inPeripheralFOV && p_distance <= l_maxRange * config.peripheralMultiplier)
        {
            return PlayerDetectionLevel.Peripheral;
        }
        
        return PlayerDetectionLevel.None;
    }
    
    #endregion
    
    #region IPlayerDetector Implementation
    
    public bool CanSeePlayer(Transform p_player)
    {
        if (!p_player) return false;
        
        var l_result = PerformDetection(p_player);
        return l_result.canSeePlayer;
    }
    
    public float GetDistanceToPlayer(Transform p_player)
    {
        if (p_player == null) return float.MaxValue;
        
        return Vector3.Distance(GetEyePosition(), p_player.position);
    }
    
    public bool IsPlayerInRange(Transform p_player, float p_range)
    {
        return GetDistanceToPlayer(p_player) <= p_range;
    }
    
    public Vector3 GetLastKnownPlayerPosition()
    {
        return m_lastKnownPlayerPosition;
    }
    
    public float GetTimeSinceLastSeen()
    {
        return m_lastSeenTime > 0 ? Time.time - m_lastSeenTime : float.MaxValue;
    }
    
    public void SetDetectionParameters(float p_detectionRange, float p_fieldOfView, LayerMask p_obstacleLayerMask)
    {
        config.detectionRange = p_detectionRange;
        config.fieldOfView = p_fieldOfView;
        config.obstacleLayerMask = p_obstacleLayerMask;
        
        if (enableDetectionLogs)
            MyLogger.LogInfo($"PlayerDetector: Updated parameters - Range: {p_detectionRange}, FOV: {p_fieldOfView}");
    }
    
    public bool CanHearPlayer(Transform p_player, float p_noiseLevel = 1f)
    {
        if (!config.useNoiseDection || p_player == null) return false;
        
        // MEJORA: Implementación básica de detección auditiva
        float l_hearingRange = config.detectionRange * 0.5f * p_noiseLevel;
        return GetDistanceToPlayer(p_player) <= l_hearingRange;
    }
    
    public Vector3 GetPredictedPlayerPosition(float p_predictionTime = 1f)
    {
        if (m_cachedPlayerTransform == null) return Vector3.zero;
        
        // MEJORA: Predicción simple basada en velocidad del player
        var l_playerRigidbody = m_cachedPlayerTransform.GetComponent<Rigidbody>();
        if (l_playerRigidbody != null)
        {
            return m_cachedPlayerTransform.position + l_playerRigidbody.linearVelocity * p_predictionTime;
        }
        
        return m_cachedPlayerTransform.position;
    }
    
    public float GetAngleToPlayer(Transform p_player)
    {
        if (p_player == null) return 0f;
        
        Vector3 l_directionToPlayer = (p_player.position - GetEyePosition()).normalized;
        return Vector3.Angle(transform.forward, l_directionToPlayer);
    }
    
    public bool IsPlayerInFieldOfView(Transform p_player)
    {
        if (p_player == null) return false;
        
        float l_angle = GetAngleToPlayer(p_player);
        return IsInFieldOfView(l_angle, config.fieldOfView);
    }
    
    public void InvalidateCache()
    {
        m_lastUpdateFrame = -1;
        m_cachedResult = DetectionResult.None;
    }
    
    public (Vector3 position, float range, float fov, bool hasLOS) GetDebugInfo()
    {
        return (GetEyePosition(), config.detectionRange, config.fieldOfView, m_lastDetectionResult.hasLineOfSight);
    }
    
    #endregion
    
    #region Helper Methods
    
    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * config.eyeHeight;
    }
    
    private bool IsInFieldOfView(float p_angle, float p_fieldOfView)
    {
        return p_angle <= p_fieldOfView * 0.5f;
    }
    
    private bool CheckLineOfSight(Vector3 p_fromPosition, Vector3 p_toPosition, out string p_blocker)
    {
        // AI Line of Sight
        p_blocker = "";
        
        Vector3 l_direction = (p_toPosition - p_fromPosition).normalized;
        float l_distance = Vector3.Distance(p_fromPosition, p_toPosition);
        
        // Main raycast
        if (Physics.Raycast(p_fromPosition, l_direction, out RaycastHit l_hit, l_distance, config.obstacleLayerMask))
        {
            // Check if we hit the player (player might be on obstacle layer)
            if (l_hit.collider.CompareTag(config.playerTag))
            {
                return true;
            }
            
            p_blocker = l_hit.collider.name;
            m_lastBlockingObject = p_blocker;
            return false;
        }
        
        // Additional raycast slightly upward for crouching players
        Vector3 l_upperDirection = (p_toPosition + Vector3.up * 0.5f - p_fromPosition).normalized;
        if (Physics.Raycast(p_fromPosition, l_upperDirection, l_distance, config.obstacleLayerMask))
        {
            return false;
        }
        
        m_lastBlockingObject = "";
        return true;
    }
    
    private void CacheResult(DetectionResult p_result)
    {
        if (config.useCache)
        {
            m_cachedResult = p_result;
            m_lastUpdateFrame = Time.frameCount;
        }
    }
    
    #endregion
    
    #region Event Handling
    
    private void OnDetectionLevelChanged(PlayerDetectionLevel p_previousLevel, PlayerDetectionLevel p_newLevel)
    {
        if (enableDetectionLogs)
            MyLogger.LogInfo($"PlayerDetector ({gameObject.name}): Detection level changed from {p_previousLevel} to {p_newLevel}");
        
        // Notify blackboard of significant changes
        if (p_newLevel >= PlayerDetectionLevel.Partial && p_previousLevel < PlayerDetectionLevel.Partial)
        {
            // Player detected for first time
            NotifyBlackboardPlayerDetected();
        }
        else if (p_newLevel < PlayerDetectionLevel.Partial && p_previousLevel >= PlayerDetectionLevel.Partial)
        {
            // Player lost
            NotifyBlackboardPlayerLost();
        }
        
        // Notify owner AI component
        NotifyOwnerAI(p_previousLevel, p_newLevel);
    }
    
    private void UpdateBlackboardWithDetection(DetectionResult p_result)
    {
        BlackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, p_result.lastKnownPosition);
        BlackboardService.SetValue(BlackboardKeys.PLAYER_LAST_SEEN, p_result.lastKnownPosition);
        BlackboardService.SetValue(BlackboardKeys.PLAYER_LAST_SEEN_TIME, Time.time);

        // Minimum scope: Update last known position when player is detected
        BlackboardService.SetValue(BlackboardKeys.LAST_KNOWN_PLAYER_POSITION, p_result.lastKnownPosition);
        
        // Update alert level based on detection
        int l_currentAlertLevel = BlackboardService.GetValue<int>(BlackboardKeys.ALERT_LEVEL);
        int l_suggestedAlertLevel = GetAlertLevelForDetection(p_result.level);
        
        if (l_suggestedAlertLevel > l_currentAlertLevel)
        {
            BlackboardService.SetValue(BlackboardKeys.ALERT_LEVEL, l_suggestedAlertLevel);
            BlackboardService.SetValue(BlackboardKeys.ALERT_POSITION, p_result.lastKnownPosition);
            BlackboardService.SetValue(BlackboardKeys.ALERT_TIME, Time.time);
            BlackboardService.SetValue(BlackboardKeys.LAST_ALERT_SOURCE, transform);
        }
    }
    
    private int GetAlertLevelForDetection(PlayerDetectionLevel p_detectionLevel)
    {
        return p_detectionLevel switch
        {
            PlayerDetectionLevel.None => 0,
            PlayerDetectionLevel.Peripheral => 1,
            PlayerDetectionLevel.Partial => 2,
            PlayerDetectionLevel.Clear => 3,
            PlayerDetectionLevel.Immediate => 3,
            _ => 0
        };
    }
    
    private void NotifyBlackboardPlayerDetected()
    {
        BlackboardService.SetValue(BlackboardKeys.PLAYER_DETECTED, true);
        
        // Add this detector to investigating list
        var l_investigating = BlackboardService.GetValue<System.Collections.Generic.List<Transform>>(BlackboardKeys.GUARDS_INVESTIGATING) 
                            ?? new System.Collections.Generic.List<Transform>();
        
        if (!l_investigating.Contains(transform))
        {
            l_investigating.Add(transform);
            BlackboardService.SetValue(BlackboardKeys.GUARDS_INVESTIGATING, l_investigating);
        }
    }
    
    private void NotifyBlackboardPlayerLost()
    {
        // Don't immediately update PLAYER_DETECTED to false - other AIs might still see player
        // Just remove this detector from investigating list
        
        var l_investigating = BlackboardService.GetValue<System.Collections.Generic.List<Transform>>(BlackboardKeys.GUARDS_INVESTIGATING);
        if (l_investigating != null && l_investigating.Contains(transform))
        {
            l_investigating.Remove(transform);
            BlackboardService.SetValue(BlackboardKeys.GUARDS_INVESTIGATING, l_investigating);
        }
    }
    
    private void NotifyOwnerAI(PlayerDetectionLevel p_previousLevel, PlayerDetectionLevel p_newLevel)
    {
        // Notify Guard component if present
        var l_guard = GetComponent<Guard>();
        if (l_guard)
        {
            l_guard.LastKnownPlayerPosition = m_lastKnownPlayerPosition;
        }
        
        // Could notify other AI components here as needed
    }
    
    private void UpdateGizmoColor(PlayerDetectionLevel p_level)
    {
        m_currentGizmoColor = p_level switch
        {
            PlayerDetectionLevel.None => Color.gray,
            PlayerDetectionLevel.Peripheral => Color.yellow,
            PlayerDetectionLevel.Partial => Color.orange,
            PlayerDetectionLevel.Clear => Color.red,
            PlayerDetectionLevel.Immediate => Color.magenta,
            _ => Color.gray
        };
    }
    
    #endregion
    
    #region Debug and Visualization
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        DrawDetectionRange();
        
        if (Application.isPlaying)
        {
            DrawFieldOfView();
            DrawLineOfSight();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        DrawDetailedDebugInfo();
    }
    
    private void DrawDetectionRange()
    {
        Vector3 l_eyePos = GetEyePosition();
        
        // Main detection range
        Gizmos.color = new Color(m_currentGizmoColor.r, m_currentGizmoColor.g, m_currentGizmoColor.b, 0.3f);
        Gizmos.DrawWireSphere(l_eyePos, config.detectionRange);
        
        // Peripheral range if enabled
        if (config.usePeripheralVision)
        {
            Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.2f);
            Gizmos.DrawWireSphere(l_eyePos, config.detectionRange * config.peripheralMultiplier);
        }
    }
    
    private void DrawFieldOfView()
    {
        Vector3 l_eyePos = GetEyePosition();
        
        // Main field of view
        Gizmos.color = m_currentGizmoColor;
        float l_halfFOV = config.fieldOfView * 0.5f;
        
        Vector3 l_leftBoundary = Quaternion.AngleAxis(-l_halfFOV, Vector3.up) * transform.forward * config.detectionRange;
        Vector3 l_rightBoundary = Quaternion.AngleAxis(l_halfFOV, Vector3.up) * transform.forward * config.detectionRange;
        
        Gizmos.DrawRay(l_eyePos, l_leftBoundary);
        Gizmos.DrawRay(l_eyePos, l_rightBoundary);
        
        // Peripheral vision if enabled
        if (config.usePeripheralVision)
        {
            Gizmos.color = Color.cyan;
            float l_peripheralHalf = 60f; // 120° total
            Vector3 l_leftPeripheral = Quaternion.AngleAxis(-l_peripheralHalf, Vector3.up) * transform.forward * (config.detectionRange * config.peripheralMultiplier);
            Vector3 l_rightPeripheral = Quaternion.AngleAxis(l_peripheralHalf, Vector3.up) * transform.forward * (config.detectionRange * config.peripheralMultiplier);
            
            Gizmos.DrawRay(l_eyePos, l_leftPeripheral);
            Gizmos.DrawRay(l_eyePos, l_rightPeripheral);
        }
    }
    
    private void DrawLineOfSight()
    {
        if (m_cachedPlayerTransform == null) return;
        
        Vector3 l_eyePos = GetEyePosition();
        Vector3 l_playerPos = m_cachedPlayerTransform.position;
        
        // Line to player
        Gizmos.color = m_lastDetectionResult.hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawLine(l_eyePos, l_playerPos);
        
        // Last known position
        if (m_lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(m_lastKnownPlayerPosition, 0.5f);
            
            // Line to last known position
            Gizmos.color = new Color(Color.orange.r, Color.orange.g, Color.orange.b, 0.5f);
            Gizmos.DrawLine(l_eyePos, m_lastKnownPlayerPosition);
        }
    }
    
    private void DrawDetailedDebugInfo()
    {
        Vector3 l_eyePos = GetEyePosition();
        
        // Eye position
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(l_eyePos, 0.1f);
        Gizmos.DrawLine(transform.position, l_eyePos);
        
        // Forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(l_eyePos, transform.forward * 2f);
        
        // Detection level indicators
        if (Application.isPlaying)
        {
            Gizmos.color = m_currentGizmoColor;
            Gizmos.DrawWireCube(l_eyePos + Vector3.up * 0.5f, Vector3.one * 0.2f);
        }
    }
    
    #endregion
    
    #region Context Menu Debug
    
    [ContextMenu("Test Detection")]
    private void TestDetection()
    {
        if (m_cachedPlayerTransform != null)
        {
            var l_result = PerformDetection(m_cachedPlayerTransform);
            MyLogger.LogInfo($"=== DETECTION TEST RESULTS ===");
            MyLogger.LogInfo($"Detection Level: {l_result.level}");
            MyLogger.LogInfo($"Can See Player: {l_result.canSeePlayer}");
            MyLogger.LogInfo($"In Field of View: {l_result.inFieldOfView}");
            MyLogger.LogInfo($"Has Line of Sight: {l_result.hasLineOfSight}");
            MyLogger.LogInfo($"Distance: {l_result.distance:F2}");
            MyLogger.LogInfo($"Angle: {l_result.angle:F1}°");
            MyLogger.LogInfo($"Blocked By: {l_result.blockedBy}");
            MyLogger.LogInfo($"Time Since Last Seen: {l_result.timeSinceLastSeen:F1}s");
        }
        else
        {
            MyLogger.LogInfo("Player not found for testing!");
        }
    }
    
    [ContextMenu("Force Player Detection")]
    private void ForcePlayerDetection()
    {
        if (m_cachedPlayerTransform != null)
        {
            m_lastKnownPlayerPosition = m_cachedPlayerTransform.position;
            m_lastSeenTime = Time.time;
            OnDetectionLevelChanged(PlayerDetectionLevel.None, PlayerDetectionLevel.Clear);
            MyLogger.LogInfo("Forced player detection!");
        }
    }
    
    [ContextMenu("Reset Detection State")]
    private void ResetDetectionState()
    {
        m_lastDetectionResult = DetectionResult.None;
        m_lastKnownPlayerPosition = Vector3.zero;
        m_lastSeenTime = -1f;
        m_previousDetectionLevel = PlayerDetectionLevel.None;
        InvalidateCache();
        MyLogger.LogInfo("Detection state reset!");
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// MEJORA: Get current detection result for AI decision making
    /// </summary>
    public DetectionResult GetCurrentDetectionResult()
    {
        return m_lastDetectionResult;
    }
    
    /// <summary>
    /// MEJORA: Runtime configuration of personality
    /// </summary>
    public void SetPersonalityType(AIPersonalityType p_newPersonality)
    {
        personalityType = p_newPersonality;
        if (autoConfigureFromPersonality)
        {
            ApplyPersonalityConfig();
        }
    }
    
    /// <summary>
    /// MEJORA: Get current configuration for external use
    /// </summary>
    public DetectionConfig GetDetectionConfig()
    {
        return config;
    }
    
    #endregion
}
