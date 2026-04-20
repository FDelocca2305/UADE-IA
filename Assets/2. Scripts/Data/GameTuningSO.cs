using Services.MicroServices.BlackboardService;
using UnityEngine;

/// <summary>
/// DEPRECATED: This configuration file is scheduled for removal.
/// Final review by SLuser is still pending before deletion.

/// Centralized tuning parameters for the AI system and game balance.
/// This ScriptableObject allows designers to easily adjust AI behavior without touching code.
/// </summary>
[CreateAssetMenu(fileName = "GameTuningSO", menuName = "AI System/Game Tuning Configuration", order = 1)]
public class GameTuningSO : ScriptableObject
{
    [Header("=== GLOBAL AI SETTINGS ===")]
    [Space(5)]
    
    [Header("Performance")]
    [SerializeField, Range(0.01f, 0.1f)] 
    private float aiUpdateInterval = 0.05f;
    
    [SerializeField] 
    private bool enablePerformanceOptimizations = true;
    
    [SerializeField, Range(1, 10)] 
    private int maxSimultaneousDetections = 5;
    
    [SerializeField] 
    private bool enableFrameCaching = true;
    
    [Header("Debug & Visualization")]
    [SerializeField] 
    private bool enableDebugGizmos = true;
    
    [SerializeField] 
    private bool enableDebugLogs = false;
    
    [SerializeField] 
    private bool showDetectionRanges = true;
    
    [SerializeField] 
    private bool showFOVCones = true;
    
    [SerializeField] 
    private bool showRaycastDebug = false;
    
    [Header("=== DETECTION SYSTEM ===")]
    [Space(5)]
    
    [Header("Base Detection Parameters")]
    [SerializeField, Range(1f, 20f)] 
    private float baseDetectionRange = 10f;
    
    [SerializeField, Range(30f, 180f)] 
    private float baseFOVAngle = 90f;
    
    [SerializeField, Range(0.5f, 3f)] 
    private float baseDetectionHeight = 1.8f;
    
    [SerializeField, Range(0.1f, 2f)] 
    private float detectionSmoothTime = 0.5f;
    
    [Header("Detection Level Thresholds")]
    [SerializeField, Range(0f, 1f)] 
    private float peripheralThreshold = 0.8f;
    
    [SerializeField, Range(0f, 1f)] 
    private float partialThreshold = 0.6f;
    
    [SerializeField, Range(0f, 1f)] 
    private float clearThreshold = 0.3f;
    
    [SerializeField, Range(0f, 1f)] 
    private float immediateThreshold = 0.1f;
    
    [Header("Raycast Configuration")]
    [SerializeField, Range(1, 10)] 
    private int raycastDensity = 3;
    
    [SerializeField] 
    private LayerMask obstacleLayerMask = 1;
    
    [SerializeField] 
    private LayerMask playerLayerMask = 1 << 8;
    
    [Header("=== PERSONALITY CONFIGURATIONS ===")]
    [Space(5)]
    
    [SerializeField] 
    private PersonalityConfig aggressiveConfig = PersonalityConfig.AggressiveDefault();
    
    [SerializeField] 
    private PersonalityConfig cautiousConfig = PersonalityConfig.CautiousDefault();
    
    [SerializeField] 
    private PersonalityConfig conservativeConfig = PersonalityConfig.ConservativeDefault();
    
    [Header("=== MOVEMENT & BEHAVIOR ===")]
    [Space(5)]
    
    [Header("Speed Configuration")]
    [SerializeField, Range(0.5f, 5f)] 
    private float basePatrolSpeed = 2f;
    
    [SerializeField, Range(1f, 8f)] 
    private float baseChaseSpeed = 4f;
    
    [SerializeField, Range(0.1f, 2f)] 
    private float baseRotationSpeed = 1f;
    
    [Header("State Timers")]
    [SerializeField, Range(1f, 10f)] 
    private float baseIdleTime = 3f;
    
    [SerializeField, Range(2f, 15f)] 
    private float baseSearchTime = 5f;
    
    [SerializeField, Range(0.5f, 5f)] 
    private float baseInvestigateTime = 3f;
    
    [Header("Combat Configuration")]
    [SerializeField, Range(1f, 5f)] 
    private float baseAttackRange = 2f;
    
    [SerializeField, Range(0.5f, 3f)] 
    private float baseCooldownTime = 1f;
    
    [SerializeField, Range(1f, 20f)] 
    private float baseProjectileSpeed = 15f;
    
    [Header("=== BLACKBOARD SETTINGS ===")]
    [Space(5)]
    
    [SerializeField, Range(0.1f, 5f)] 
    private float blackboardUpdateInterval = 0.1f;
    
    [SerializeField, Range(5f, 60f)] 
    private float memoryRetentionTime = 15f;
    
    [SerializeField] 
    private bool enableSharedMemory = true;
    
    [Header("=== DIFFICULTY PRESETS ===")]
    [Space(5)]
    
    [SerializeField] 
    private DifficultyPreset easyPreset = DifficultyPreset.EasyDefault();
    
    [SerializeField] 
    private DifficultyPreset normalPreset = DifficultyPreset.NormalDefault();
    
    [SerializeField] 
    private DifficultyPreset hardPreset = DifficultyPreset.HardDefault();
    
    #region Public Properties
    
    // Performance
    public float AIUpdateInterval => aiUpdateInterval;
    public bool EnablePerformanceOptimizations => enablePerformanceOptimizations;
    public int MaxSimultaneousDetections => maxSimultaneousDetections;
    public bool EnableFrameCaching => enableFrameCaching;
    
    // Debug
    public bool EnableDebugGizmos => enableDebugGizmos;
    public bool EnableDebugLogs => enableDebugLogs;
    public bool ShowDetectionRanges => showDetectionRanges;
    public bool ShowFOVCones => showFOVCones;
    public bool ShowRaycastDebug => showRaycastDebug;
    
    // Detection
    public float BaseDetectionRange => baseDetectionRange;
    public float BaseFOVAngle => baseFOVAngle;
    public float BaseDetectionHeight => baseDetectionHeight;
    public float DetectionSmoothTime => detectionSmoothTime;
    
    public float PeripheralThreshold => peripheralThreshold;
    public float PartialThreshold => partialThreshold;
    public float ClearThreshold => clearThreshold;
    public float ImmediateThreshold => immediateThreshold;
    
    public int RaycastDensity => raycastDensity;
    public LayerMask ObstacleLayerMask => obstacleLayerMask;
    public LayerMask PlayerLayerMask => playerLayerMask;
    
    // Movement
    public float BasePatrolSpeed => basePatrolSpeed;
    public float BaseChaseSpeed => baseChaseSpeed;
    public float BaseRotationSpeed => baseRotationSpeed;
    
    // Timers
    public float BaseIdleTime => baseIdleTime;
    public float BaseSearchTime => baseSearchTime;
    public float BaseInvestigateTime => baseInvestigateTime;
    
    // Combat
    public float BaseAttackRange => baseAttackRange;
    public float BaseCooldownTime => baseCooldownTime;
    public float BaseProjectileSpeed => baseProjectileSpeed;
    
    // Blackboard
    public float BlackboardUpdateInterval => blackboardUpdateInterval;
    public float MemoryRetentionTime => memoryRetentionTime;
    public bool EnableSharedMemory => enableSharedMemory;
    
    #endregion
    
    #region Personality System
    
    public PersonalityConfig GetPersonalityConfig(AIPersonalityType personalityType)
    {
        return personalityType switch
        {
            AIPersonalityType.Aggressive => aggressiveConfig,
            AIPersonalityType.Cautious => cautiousConfig,
            AIPersonalityType.Conservative => conservativeConfig,
            _ => cautiousConfig
        };
    }
    
    public DetectionConfig GetDetectionConfig(AIPersonalityType personalityType)
    {
        var personality = GetPersonalityConfig(personalityType);
        
        return new DetectionConfig
        {
            detectionRange = baseDetectionRange * personality.detectionRangeMultiplier,
            fovAngle = baseFOVAngle * personality.fovMultiplier,
            detectionHeight = baseDetectionHeight,
            raycastDensity = Mathf.RoundToInt(raycastDensity * personality.accuracyMultiplier),
            obstacleLayerMask = obstacleLayerMask,
            playerLayerMask = playerLayerMask,
            smoothTime = detectionSmoothTime * personality.reactionTimeMultiplier,
            
            peripheralThreshold = peripheralThreshold * personality.alertnessMultiplier,
            partialThreshold = partialThreshold * personality.alertnessMultiplier,
            clearThreshold = clearThreshold * personality.alertnessMultiplier,
            immediateThreshold = immediateThreshold * personality.alertnessMultiplier
        };
    }
    
    #endregion
    
    #region Difficulty System
    
    public void ApplyDifficultyPreset(DifficultyLevel difficulty)
    {
        var preset = difficulty switch
        {
            DifficultyLevel.Easy => easyPreset,
            DifficultyLevel.Normal => normalPreset,
            DifficultyLevel.Hard => hardPreset,
            _ => normalPreset
        };
        
        ApplyPreset(preset);
    }
    
    private void ApplyPreset(DifficultyPreset preset)
    {
        // Detection
        baseDetectionRange *= preset.detectionRangeMultiplier;
        baseFOVAngle *= preset.fovMultiplier;
        detectionSmoothTime *= preset.reactionTimeMultiplier;
        
        // Movement
        basePatrolSpeed *= preset.speedMultiplier;
        baseChaseSpeed *= preset.speedMultiplier;
        
        // Combat
        baseAttackRange *= preset.attackRangeMultiplier;
        baseCooldownTime *= preset.cooldownMultiplier;
        
        // Alertness
        peripheralThreshold *= preset.alertnessMultiplier;
        partialThreshold *= preset.alertnessMultiplier;
        clearThreshold *= preset.alertnessMultiplier;
        immediateThreshold *= preset.alertnessMultiplier;
        
        MyLogger.LogInfo($"Applied difficulty preset: {preset.name}");
    }
    
    #endregion
    
    #region Validation
    
    private void OnValidate()
    {
        // Clamp values to safe ranges
        aiUpdateInterval = Mathf.Clamp(aiUpdateInterval, 0.01f, 0.1f);
        maxSimultaneousDetections = Mathf.Clamp(maxSimultaneousDetections, 1, 10);
        
        baseDetectionRange = Mathf.Clamp(baseDetectionRange, 1f, 20f);
        baseFOVAngle = Mathf.Clamp(baseFOVAngle, 30f, 180f);
        baseDetectionHeight = Mathf.Clamp(baseDetectionHeight, 0.5f, 3f);
        
        // Ensure thresholds are in correct order
        immediateThreshold = Mathf.Clamp01(immediateThreshold);
        clearThreshold = Mathf.Clamp(clearThreshold, immediateThreshold, 1f);
        partialThreshold = Mathf.Clamp(partialThreshold, clearThreshold, 1f);
        peripheralThreshold = Mathf.Clamp(peripheralThreshold, partialThreshold, 1f);
        
        raycastDensity = Mathf.Clamp(raycastDensity, 1, 10);
        
        // Validate personality configs
        aggressiveConfig.Validate();
        cautiousConfig.Validate();
        conservativeConfig.Validate();
    }
    
    #endregion
    
    #region Debug Utilities
    
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        // Performance
        aiUpdateInterval = 0.05f;
        enablePerformanceOptimizations = true;
        maxSimultaneousDetections = 5;
        enableFrameCaching = true;
        
        // Detection
        baseDetectionRange = 10f;
        baseFOVAngle = 90f;
        baseDetectionHeight = 1.8f;
        detectionSmoothTime = 0.5f;
        
        peripheralThreshold = 0.8f;
        partialThreshold = 0.6f;
        clearThreshold = 0.3f;
        immediateThreshold = 0.1f;
        
        raycastDensity = 3;
        
        // Movement
        basePatrolSpeed = 2f;
        baseChaseSpeed = 4f;
        baseRotationSpeed = 1f;
        
        // Timers
        baseIdleTime = 3f;
        baseSearchTime = 5f;
        baseInvestigateTime = 3f;
        
        // Combat
        baseAttackRange = 2f;
        baseCooldownTime = 1f;
        baseProjectileSpeed = 15f;
        
        // Blackboard
        blackboardUpdateInterval = 0.1f;
        memoryRetentionTime = 15f;
        enableSharedMemory = true;
        
        // Reset personality configs
        aggressiveConfig = PersonalityConfig.AggressiveDefault();
        cautiousConfig = PersonalityConfig.CautiousDefault();
        conservativeConfig = PersonalityConfig.ConservativeDefault();
        
        // Reset difficulty presets
        easyPreset = DifficultyPreset.EasyDefault();
        normalPreset = DifficultyPreset.NormalDefault();
        hardPreset = DifficultyPreset.HardDefault();
        
        MyLogger.LogInfo("GameTuningSO: Reset to default values");
    }
    
    [ContextMenu("Print Current Configuration")]
    public void PrintCurrentConfiguration()
    {
        MyLogger.LogInfo("=== GAME TUNING CONFIGURATION ===");
        MyLogger.LogInfo($"AI Update Interval: {aiUpdateInterval}s");
        MyLogger.LogInfo($"Base Detection Range: {baseDetectionRange}");
        MyLogger.LogInfo($"Base FOV Angle: {baseFOVAngle}°");
        MyLogger.LogInfo($"Base Patrol Speed: {basePatrolSpeed}");
        MyLogger.LogInfo($"Base Chase Speed: {baseChaseSpeed}");
        MyLogger.LogInfo($"Peripheral Threshold: {peripheralThreshold}");
        MyLogger.LogInfo($"Partial Threshold: {partialThreshold}");
        MyLogger.LogInfo($"Clear Threshold: {clearThreshold}");
        MyLogger.LogInfo($"Immediate Threshold: {immediateThreshold}");
        MyLogger.LogInfo("================================");
    }
    
    #endregion
}

#region Supporting Structs

[System.Serializable]
public struct PersonalityConfig
{
    [Header("Detection Modifiers")]
    [Range(0.5f, 2f)] public float detectionRangeMultiplier;
    [Range(0.5f, 2f)] public float fovMultiplier;
    [Range(0.5f, 2f)] public float alertnessMultiplier;
    [Range(0.5f, 2f)] public float accuracyMultiplier;
    
    [Header("Behavior Modifiers")]
    [Range(0.5f, 2f)] public float aggressionMultiplier;
    [Range(0.5f, 2f)] public float reactionTimeMultiplier;
    [Range(0.5f, 2f)] public float persistenceMultiplier;
    
    [Header("Movement Modifiers")]
    [Range(0.5f, 2f)] public float speedMultiplier;
    [Range(0.5f, 2f)] public float rotationSpeedMultiplier;
    
    public static PersonalityConfig AggressiveDefault()
    {
        return new PersonalityConfig
        {
            detectionRangeMultiplier = 1.2f,
            fovMultiplier = 1.1f,
            alertnessMultiplier = 1.3f,
            accuracyMultiplier = 1.0f,
            aggressionMultiplier = 1.5f,
            reactionTimeMultiplier = 0.7f,
            persistenceMultiplier = 1.4f,
            speedMultiplier = 1.2f,
            rotationSpeedMultiplier = 1.3f
        };
    }
    
    public static PersonalityConfig CautiousDefault()
    {
        return new PersonalityConfig
        {
            detectionRangeMultiplier = 1.0f,
            fovMultiplier = 1.0f,
            alertnessMultiplier = 1.0f,
            accuracyMultiplier = 1.1f,
            aggressionMultiplier = 1.0f,
            reactionTimeMultiplier = 1.0f,
            persistenceMultiplier = 1.0f,
            speedMultiplier = 1.0f,
            rotationSpeedMultiplier = 1.0f
        };
    }
    
    public static PersonalityConfig ConservativeDefault()
    {
        return new PersonalityConfig
        {
            detectionRangeMultiplier = 0.8f,
            fovMultiplier = 0.9f,
            alertnessMultiplier = 0.8f,
            accuracyMultiplier = 1.2f,
            aggressionMultiplier = 0.7f,
            reactionTimeMultiplier = 1.3f,
            persistenceMultiplier = 0.8f,
            speedMultiplier = 0.9f,
            rotationSpeedMultiplier = 0.8f
        };
    }
    
    public void Validate()
    {
        detectionRangeMultiplier = Mathf.Clamp(detectionRangeMultiplier, 0.5f, 2f);
        fovMultiplier = Mathf.Clamp(fovMultiplier, 0.5f, 2f);
        alertnessMultiplier = Mathf.Clamp(alertnessMultiplier, 0.5f, 2f);
        accuracyMultiplier = Mathf.Clamp(accuracyMultiplier, 0.5f, 2f);
        aggressionMultiplier = Mathf.Clamp(aggressionMultiplier, 0.5f, 2f);
        reactionTimeMultiplier = Mathf.Clamp(reactionTimeMultiplier, 0.5f, 2f);
        persistenceMultiplier = Mathf.Clamp(persistenceMultiplier, 0.5f, 2f);
        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 2f);
        rotationSpeedMultiplier = Mathf.Clamp(rotationSpeedMultiplier, 0.5f, 2f);
    }
}

[System.Serializable]
public struct DifficultyPreset
{
    public string name;
    [Range(0.5f, 2f)] public float detectionRangeMultiplier;
    [Range(0.5f, 2f)] public float fovMultiplier;
    [Range(0.5f, 2f)] public float alertnessMultiplier;
    [Range(0.5f, 2f)] public float reactionTimeMultiplier;
    [Range(0.5f, 2f)] public float speedMultiplier;
    [Range(0.5f, 2f)] public float attackRangeMultiplier;
    [Range(0.5f, 2f)] public float cooldownMultiplier;
    
    public static DifficultyPreset EasyDefault()
    {
        return new DifficultyPreset
        {
            name = "Easy",
            detectionRangeMultiplier = 0.7f,
            fovMultiplier = 0.8f,
            alertnessMultiplier = 0.7f,
            reactionTimeMultiplier = 1.5f,
            speedMultiplier = 0.8f,
            attackRangeMultiplier = 0.8f,
            cooldownMultiplier = 1.3f
        };
    }
    
    public static DifficultyPreset NormalDefault()
    {
        return new DifficultyPreset
        {
            name = "Normal",
            detectionRangeMultiplier = 1.0f,
            fovMultiplier = 1.0f,
            alertnessMultiplier = 1.0f,
            reactionTimeMultiplier = 1.0f,
            speedMultiplier = 1.0f,
            attackRangeMultiplier = 1.0f,
            cooldownMultiplier = 1.0f
        };
    }
    
    public static DifficultyPreset HardDefault()
    {
        return new DifficultyPreset
        {
            name = "Hard",
            detectionRangeMultiplier = 1.3f,
            fovMultiplier = 1.2f,
            alertnessMultiplier = 1.4f,
            reactionTimeMultiplier = 0.7f,
            speedMultiplier = 1.3f,
            attackRangeMultiplier = 1.2f,
            cooldownMultiplier = 0.7f
        };
    }
}

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

#endregion
