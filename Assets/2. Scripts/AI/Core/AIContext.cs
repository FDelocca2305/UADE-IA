using Services.MicroServices.BlackboardService;
using Services;
using Unity.Assertions;
using UnityEngine;

/// <summary>
/// Concrete implementation of IAIContext that centralizes access to AI data.
/// 
/// IMPROVEMENT: Smart per-frame cache to avoid redundant calculations.
/// IMPROVEMENT: Full integration with PlayerDetector and Blackboard.
/// </summary>
public class AIContext : MonoBehaviour, IAIContext
{
    [Header("AI Configuration")]
    [SerializeField] private AIPersonalityType personalityType = AIPersonalityType.Conservative;
    [SerializeField] private string primaryTargetTag = "Player";
    [SerializeField] private PlayerDetector playerDetector;
    
    [Header("Performance")]
    [SerializeField] private bool enableCaching = true;
    [SerializeField] private bool autoInvalidateCache = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Component references
    private IAIMovementController movementController;
    
    // Cache system
    private int cacheFrame = -1;
    private bool cachedPlayerVisible;
    private Vector3 cachedPlayerPosition;
    private float cachedDistanceToPlayer;
    private Transform cachedPlayerTransform;
    private Transform cachedTarget;
    
    private static IBlackboardService BlackboardService => ServiceLocator.Get<IBlackboardService>();
    
    // Multiple target support
    private System.Collections.Generic.Dictionary<string, Transform> targetCache = 
        new System.Collections.Generic.Dictionary<string, Transform>();
    private System.Collections.Generic.Dictionary<string, float> distanceCache = 
        new System.Collections.Generic.Dictionary<string, float>();
    private System.Collections.Generic.Dictionary<string, bool> visibilityCache = 
        new System.Collections.Generic.Dictionary<string, bool>();
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeComponents();
        InitializeBlackboardConnection();
    }
    
    private void Update()
    {
        if (autoInvalidateCache && Time.frameCount != cacheFrame)
        {
            InvalidateCache();
        }
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeComponents()
    {
        Assert.IsNotNull(playerDetector);
        
        movementController = GetComponent<IAIMovementController>();
        if (movementController == null && enableDebugLogs)
        {
            MyLogger.LogWarning($"AIContext on {gameObject.name}: IAIMovementController not found");
        }
    }
    
    private void InitializeBlackboardConnection()
    {
        // Subscribe to relevant blackboard changes for cache invalidation
        if (enableCaching)
        {
            BlackboardService.Subscribe<Transform>(BlackboardKeys.PLAYER_TRANSFORM, OnPlayerTransformChanged);
            BlackboardService.Subscribe<Vector3>(BlackboardKeys.PLAYER_POSITION, OnPlayerPositionChanged);
        }
        
        if (enableDebugLogs)
            MyLogger.LogInfo($"AIContext on {gameObject.name}: Initialized with personality {personalityType}");
    }
    
    #endregion
    
    #region IAIContext Implementation
    
    public Transform GetTransform()
    {
        return transform;
    }
    
    public IBlackboardService GetBlackboard()
    {
        return BlackboardService;
    }
    
    public bool IsPlayerVisible()
    {
        if (enableCaching && IsCacheValid())
        {
            return cachedPlayerVisible;
        }
        
        var player = GetTarget(primaryTargetTag);
        if (player == null)
        {
            cachedPlayerVisible = false;
        }
        else
        {
            cachedPlayerVisible = playerDetector?.CanSeePlayer(player) ?? false;
        }
        
        UpdateCacheFrame();
        return cachedPlayerVisible;
    }
    
    public Vector3 GetPlayerPosition()
    {
        if (enableCaching && IsCacheValid())
        {
            return cachedPlayerPosition;
        }
        
        var player = GetTarget(primaryTargetTag);
        if (player != null)
        {
            cachedPlayerPosition = player.position;
        }
        else
        {
            // Fallback to blackboard
            cachedPlayerPosition = BlackboardService.GetValue<Vector3>(BlackboardKeys.PLAYER_POSITION);
        }
        
        UpdateCacheFrame();
        return cachedPlayerPosition;
    }
    
    public float GetDistanceToPlayer()
    {
        if (enableCaching && IsCacheValid())
        {
            return cachedDistanceToPlayer;
        }
        
        var player = GetTarget(primaryTargetTag);
        if (player != null)
        {
            cachedDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        }
        else
        {
            cachedDistanceToPlayer = float.MaxValue;
        }
        
        UpdateCacheFrame();
        return cachedDistanceToPlayer;
    }
    
    public AIPersonalityType GetPersonalityType()
    {
        return personalityType;
    }
    
    #endregion
    
    #region Extended IAIContext Implementation
    
    public Transform GetTarget(string targetTag = "Player")
    {
        // Check cache first
        if (enableCaching && targetCache.TryGetValue(targetTag, out Transform cachedTarget) && cachedTarget != null)
        {
            return cachedTarget;
        }
        
        Transform target = null;
        
        // Primary target (player) - try blackboard first
        if (targetTag == primaryTargetTag)
        {
            target = BlackboardService.GetValue<Transform>(BlackboardKeys.PLAYER_TRANSFORM);
        }
        
        // Fallback to finding by tag
        if (!target)
        {
            var targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject)
            {
                target = targetObject.transform;
                
                // Update blackboard if this is the primary target
                if (targetTag == primaryTargetTag)
                {
                    BlackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, target);
                }
            }
        }
        
        // Cache the result
        if (enableCaching)
        {
            targetCache[targetTag] = target;
        }
        
        return target;
    }
    
    public float GetDistanceToTarget(string targetTag = "Player")
    {
        // Check cache first
        if (enableCaching && distanceCache.TryGetValue(targetTag, out float cachedDistance))
        {
            return cachedDistance;
        }
        
        var target = GetTarget(targetTag);
        float distance = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
        
        // Cache the result
        if (enableCaching)
        {
            distanceCache[targetTag] = distance;
        }
        
        return distance;
    }
    
    public bool IsTargetVisible(string targetTag = "Player")
    {
        // Check cache first
        if (enableCaching && visibilityCache.TryGetValue(targetTag, out bool cachedVisibility))
        {
            return cachedVisibility;
        }
        
        var target = GetTarget(targetTag);
        bool isVisible = target != null && (playerDetector?.CanSeePlayer(target) ?? false);
        
        // Cache the result
        if (enableCaching)
        {
            visibilityCache[targetTag] = isVisible;
        }
        
        return isVisible;
    }
    
    public IPlayerDetector GetPlayerDetector()
    {
        return playerDetector;
    }
    
    public IAIMovementController GetMovementController()
    {
        return movementController;
    }
    
    public int GetCacheFrame()
    {
        return cacheFrame;
    }
    
    public void InvalidateCache()
    {
        cacheFrame = -1;
        targetCache.Clear();
        distanceCache.Clear();
        visibilityCache.Clear();
        
        if (enableDebugLogs)
            MyLogger.LogDebug($"AIContext on {gameObject.name}: Cache invalidated");
    }
    
    #endregion
    
    #region Cache Management
    
    private bool IsCacheValid()
    {
        return enableCaching && cacheFrame == Time.frameCount;
    }
    
    private void UpdateCacheFrame()
    {
        if (enableCaching)
        {
            cacheFrame = Time.frameCount;
        }
    }
    
    private void OnPlayerTransformChanged(Transform newPlayerTransform)
    {
        cachedPlayerTransform = newPlayerTransform;
        InvalidateCache();
        
        if (enableDebugLogs)
            MyLogger.LogDebug($"AIContext: Player transform changed, cache invalidated");
    }
    
    private void OnPlayerPositionChanged(Vector3 newPosition)
    {
        // Only invalidate distance/position cache, not visibility (that's more expensive)
        if (enableCaching)
        {
            distanceCache.Clear();
            cachedPlayerPosition = newPosition;
            cachedDistanceToPlayer = Vector3.Distance(transform.position, newPosition);
        }
    }
    
    #endregion
    
    #region Public API Extensions
    
    /// <summary>
    /// Set personality type at runtime
    /// </summary>
    public void SetPersonalityType(AIPersonalityType newPersonality)
    {
        if (personalityType != newPersonality)
        {
            personalityType = newPersonality;
            
            // Update PlayerDetector personality if it supports it
            playerDetector.SetPersonalityType(newPersonality);
            
            if (enableDebugLogs)
                MyLogger.LogInfo($"AIContext: Personality changed to {newPersonality}");
        }
    }
    
    /// <summary>
    /// Get detailed detection information
    /// </summary>
    public DetectionResult GetDetectionResult()
    {
        if (playerDetector != null)
        {
            return playerDetector.GetCurrentDetectionResult();
        }
        
        return DetectionResult.None;
    }
    
    /// <summary>
    /// Check if any target is visible
    /// </summary>
    public bool IsAnyTargetVisible(params string[] targetTags)
    {
        foreach (string tag in targetTags)
        {
            if (IsTargetVisible(tag))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get closest target from a list
    /// </summary>
    public Transform GetClosestTarget(params string[] targetTags)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (string tag in targetTags)
        {
            var target = GetTarget(tag);
            if (target != null)
            {
                float distance = GetDistanceToTarget(tag);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = target;
                }
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Get threat level based on detection and distance
    /// </summary>
    public float GetThreatLevel()
    {
        if (!IsPlayerVisible())
        {
            return 0f;
        }
        
        var detectionResult = GetDetectionResult();
        float distance = GetDistanceToPlayer();
        
        // Base threat from detection level
        float baseThreat = detectionResult.level switch
        {
            PlayerDetectionLevel.Peripheral => 0.2f,
            PlayerDetectionLevel.Partial => 0.4f,
            PlayerDetectionLevel.Clear => 0.7f,
            PlayerDetectionLevel.Immediate => 1.0f,
            _ => 0f
        };
        
        // Modify by distance (closer = more threatening)
        float maxRange = playerDetector?.GetDetectionConfig().detectionRange ?? 10f;
        float distanceFactor = 1f - Mathf.Clamp01(distance / maxRange);
        
        return baseThreat * (0.5f + 0.5f * distanceFactor);
    }
    
    /// <summary>
    /// Get confidence level in current information
    /// </summary>
    public float GetInformationConfidence()
    {
        var detectionResult = GetDetectionResult();
        float timeSinceLastSeen = detectionResult.timeSinceLastSeen;
        
        if (timeSinceLastSeen <= 0.1f)
        {
            return 1.0f; // Very recent
        }
        else if (timeSinceLastSeen <= 1f)
        {
            return 0.8f; // Recent
        }
        else if (timeSinceLastSeen <= 5f)
        {
            return 0.5f; // Somewhat recent
        }
        else if (timeSinceLastSeen <= 15f)
        {
            return 0.2f; // Old information
        }
        else
        {
            return 0f; // Very old information
        }
    }
    
    #endregion
    
    #region Debug and Diagnostics
    
    [ContextMenu("Print Current Context")]
    private void PrintCurrentContext()
    {
        MyLogger.LogInfo("=== AI CONTEXT STATUS ===");
        MyLogger.LogInfo($"Personality: {personalityType}");
        MyLogger.LogInfo($"Player Visible: {IsPlayerVisible()}");
        MyLogger.LogInfo($"Player Position: {GetPlayerPosition()}");
        MyLogger.LogInfo($"Distance to Player: {GetDistanceToPlayer():F2}");
        MyLogger.LogInfo($"Threat Level: {GetThreatLevel():F2}");
        MyLogger.LogInfo($"Information Confidence: {GetInformationConfidence():F2}");
        MyLogger.LogInfo($"Cache Frame: {cacheFrame} (Current: {Time.frameCount})");
        
        var detectionResult = GetDetectionResult();
        MyLogger.LogInfo($"Detection Level: {detectionResult.level}");
        MyLogger.LogInfo($"Time Since Last Seen: {detectionResult.timeSinceLastSeen:F1}s");
        if (!string.IsNullOrEmpty(detectionResult.blockedBy))
        {
            MyLogger.LogInfo($"Blocked By: {detectionResult.blockedBy}");
        }
        MyLogger.LogInfo("========================");
    }
    
    [ContextMenu("Force Cache Refresh")]
    private void ForceCacheRefresh()
    {
        InvalidateCache();
        
        // Force update of all cached values
        _ = IsPlayerVisible();
        _ = GetPlayerPosition();
        _ = GetDistanceToPlayer();
        
        MyLogger.LogInfo("AIContext: Cache forcefully refreshed");
    }
    
    [ContextMenu("Test All Targets")]
    private void TestAllTargets()
    {
        string[] testTags = { "Player", "Enemy", "Civilian", "Guard" };
        
        MyLogger.LogInfo("=== TARGET TEST RESULTS ===");
        foreach (string tag in testTags)
        {
            var target = GetTarget(tag);
            if (target != null)
            {
                float distance = GetDistanceToTarget(tag);
                bool visible = IsTargetVisible(tag);
                MyLogger.LogInfo($"{tag}: Found at {distance:F2} units, Visible: {visible}");
            }
            else
            {
                MyLogger.LogInfo($"{tag}: Not found");
            }
        }
        MyLogger.LogInfo("===========================");
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Unsubscribe from blackboard events
        if (!enableCaching) return;
        
        BlackboardService.Unsubscribe<Transform>(BlackboardKeys.PLAYER_TRANSFORM, OnPlayerTransformChanged);
        BlackboardService.Unsubscribe<Vector3>(BlackboardKeys.PLAYER_POSITION, OnPlayerPositionChanged);
    }
    
    #endregion
}
