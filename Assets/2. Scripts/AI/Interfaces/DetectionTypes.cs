using Services.MicroServices.BlackboardService;
using UnityEngine;

/// <summary>
/// Levels of player detection for different AI responses.
/// Used for graduated AI reactions and debugging.
/// </summary>
public enum PlayerDetectionLevel
{
    /// <summary>
    /// No player detected - normal patrol behavior
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Player barely visible or at edge of vision - suspicious behavior
    /// </summary>
    Peripheral = 1,
    
    /// <summary>
    /// Player partially visible but with obstacles - investigating behavior
    /// </summary>
    Partial = 2,
    
    /// <summary>
    /// Player clearly visible with line of sight - alert/pursuit behavior
    /// </summary>
    Clear = 3,
    
    /// <summary>
    /// Player very close and clearly visible - combat behavior
    /// </summary>
    Immediate = 4
}

/// <summary>
/// Result structure for detection queries providing detailed information.
/// 
/// MEJORA: Estructura completa para decisiones AI informadas
/// MEJORA: Cache de resultados para performance
/// </summary>
[System.Serializable]
public struct DetectionResult
{
    public PlayerDetectionLevel level;
    public bool canSeePlayer;
    public bool inFieldOfView;
    public bool hasLineOfSight;
    public float distance;
    public float angle;
    public Vector3 lastKnownPosition;
    public float timeSinceLastSeen;
    public string blockedBy; // Name of blocking object
    
    /// <summary>
    /// MEJORA: Constructor para fácil creación
    /// </summary>
    public DetectionResult(PlayerDetectionLevel level, bool canSee, bool inFOV, bool hasLOS, 
                          float dist, float ang, Vector3 lastPos, float timeSince, string blocker = "")
    {
        this.level = level;
        this.canSeePlayer = canSee;
        this.inFieldOfView = inFOV;
        this.hasLineOfSight = hasLOS;
        this.distance = dist;
        this.angle = ang;
        this.lastKnownPosition = lastPos;
        this.timeSinceLastSeen = timeSince;
        this.blockedBy = blocker;
    }
    
    /// <summary>
    /// MEJORA: Resultado por defecto para cuando no hay detección
    /// </summary>
    public static DetectionResult None => new DetectionResult(
        PlayerDetectionLevel.None, false, false, false, 
        float.MaxValue, 0f, Vector3.zero, float.MaxValue, ""
    );
    
    /// <summary>
    /// MEJORA: Resultado para detección clara
    /// </summary>
    public static DetectionResult Clear => new DetectionResult(
        PlayerDetectionLevel.Clear, true, true, true,
        0f, 0f, Vector3.zero, 0f, ""
    );
    
    /// <summary>
    /// MEJORA: Resultado para detección periférica
    /// </summary>
    public static DetectionResult Peripheral => new DetectionResult(
        PlayerDetectionLevel.Peripheral, true, true, false,
        0f, 0f, Vector3.zero, 0f, ""
    );
    
    /// <summary>
    /// MEJORA: Resultado para detección parcial
    /// </summary>
    public static DetectionResult Partial => new DetectionResult(
        PlayerDetectionLevel.Partial, true, true, true,
        0f, 0f, Vector3.zero, 0f, ""
    );
    
    /// <summary>
    /// MEJORA: Resultado para detección inmediata
    /// </summary>
    public static DetectionResult Immediate => new DetectionResult(
        PlayerDetectionLevel.Immediate, true, true, true,
        0f, 0f, Vector3.zero, 0f, ""
    );
    
    /// <summary>
    /// MEJORA: Check rápido si la detección es significativa
    /// </summary>
    public bool IsSignificant => level >= PlayerDetectionLevel.Partial;
    
    /// <summary>
    /// MEJORA: Check si requiere acción inmediata
    /// </summary>
    public bool RequiresImmediateAction => level >= PlayerDetectionLevel.Clear;
}

/// <summary>
/// Configuration data for detection parameters.
/// Allows per-AI customization while maintaining consistent interfaces.
/// 
/// MEJORA: Configuración centralizada para diferentes tipos de AI
/// MEJORA: Support para personality-based modifications
/// </summary>
[System.Serializable]
public struct DetectionConfig
{
    [Header("Basic Detection")]
    public float detectionRange;
    public float fieldOfView;
    public float fovAngle;  // Alias for fieldOfView
    public float eyeHeight;
    public float detectionHeight;  // Alias for eyeHeight
    
    [Header("Performance")]
    public float updateRate;
    public bool useCache;
    public int raycastDensity;
    public float smoothTime;
    
    [Header("Detection Thresholds")]
    public float peripheralThreshold;
    public float partialThreshold;
    public float clearThreshold;
    public float immediateThreshold;
    
    [Header("Advanced")]
    public bool usePeripheralVision;
    public float peripheralMultiplier;
    public bool useLightingAffection;
    public bool useNoiseDection;
    
    [Header("Layers")]
    public LayerMask obstacleLayerMask;
    public LayerMask playerLayerMask;
    public string playerTag;
    
    /// <summary>
    /// MEJORA: Configuración por defecto basada en personalidad
    /// </summary>
    public static DetectionConfig GetDefault(AIPersonalityType personality)
    {
        var config = new DetectionConfig
        {
            detectionRange = 8f,
            fieldOfView = 90f,
            fovAngle = 90f,  // Same as fieldOfView
            eyeHeight = 1.6f,
            detectionHeight = 1.6f,  // Same as eyeHeight
            updateRate = 0.1f,
            useCache = true,
            raycastDensity = 3,
            smoothTime = 0.5f,
            
            // Detection thresholds
            peripheralThreshold = 0.8f,
            partialThreshold = 0.6f,
            clearThreshold = 0.3f,
            immediateThreshold = 0.1f,
            
            usePeripheralVision = true,
            peripheralMultiplier = 0.5f,
            useLightingAffection = false,
            useNoiseDection = false,
            obstacleLayerMask = 1, // Default layer
            playerLayerMask = 1 << 6, // Player layer
            playerTag = "Player"
        };
        
        // MEJORA: Personalización automática basada en tipo
        switch (personality)
        {
            case AIPersonalityType.Aggressive:
                config.detectionRange *= 1.2f;
                config.fieldOfView += 20f;
                config.fovAngle += 20f;
                config.updateRate = 0.08f; // Más frecuente
                break;
                
            case AIPersonalityType.Conservative:
                config.detectionRange *= 0.9f;
                config.fieldOfView -= 15f;
                config.fovAngle -= 15f;
                config.updateRate = 0.12f; // Menos frecuente
                break;
                
            case AIPersonalityType.Cautious:
                config.detectionRange *= 1.0f;
                config.fieldOfView *= 1.0f;
                config.fovAngle *= 1.0f;
                config.updateRate = 0.1f; // Standard
                break;
                
            case AIPersonalityType.Elite:
                config.detectionRange *= 1.5f;
                config.fieldOfView += 30f;
                config.fovAngle += 30f;
                config.updateRate = 0.06f; // Muy frecuente
                config.usePeripheralVision = true;
                config.useLightingAffection = true;
                break;
                
            case AIPersonalityType.Civilian:
                config.detectionRange *= 0.7f;
                config.fieldOfView += 30f; // Más conscientes pero menor rango
                config.updateRate = 0.15f;
                break;
        }
        
        return config;
    }
}

/// <summary>
/// MEJORA: Event data para notificaciones de detección
/// Permite respuestas coordinadas entre AIs
/// </summary>
[System.Serializable]
public struct DetectionEvent
{
    public Transform detector;
    public Transform target;
    public PlayerDetectionLevel level;
    public Vector3 position;
    public float timestamp;
    public bool wasFirstDetection;
    
    public DetectionEvent(Transform detector, Transform target, PlayerDetectionLevel level, 
                         Vector3 pos, bool firstTime = false)
    {
        this.detector = detector;
        this.target = target;
        this.level = level;
        this.position = pos;
        this.timestamp = Time.time;
        this.wasFirstDetection = firstTime;
    }
}