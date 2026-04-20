using Services.MicroServices.BlackboardService;
using UnityEngine;

/// <summary>
/// Interface for consistent player detection and line-of-sight calculations across all NPCs.
/// Provides standardized vision system that can be configured per AI type.
/// 
/// MEJORA: Agregado soporte para diferentes tipos de detección (visual, auditiva, etc.)
/// MEJORA: Cache interno para evitar raycast repetidos
/// MEJORA: Integración con el sistema de alerta del blackboard
/// </summary>
public interface IPlayerDetector
{
    /// <summary>
    /// Checks if the player can be seen from current position
    /// </summary>
    /// <param name="player">Player transform to check visibility for</param>
    /// <returns>True if player is visible</returns>
    bool CanSeePlayer(Transform player);
    
    /// <summary>
    /// Gets the distance to the player
    /// </summary>
    /// <param name="player">Player transform to measure distance to</param>
    /// <returns>Distance in world units</returns>
    float GetDistanceToPlayer(Transform player);
    
    /// <summary>
    /// Checks if player is within a specific range
    /// </summary>
    /// <param name="player">Player transform to check</param>
    /// <param name="range">Range to check within</param>
    /// <returns>True if player is in range</returns>
    bool IsPlayerInRange(Transform player, float range);
    
    /// <summary>
    /// Gets the last known position where the player was seen
    /// </summary>
    /// <returns>Last known player position</returns>
    Vector3 GetLastKnownPlayerPosition();
    
    /// <summary>
    /// Gets time since player was last seen
    /// </summary>
    /// <returns>Time in seconds since last detection</returns>
    float GetTimeSinceLastSeen();
    
    /// <summary>
    /// MEJORA: Configurar parámetros de detección en runtime
    /// Permite personalizar comportamiento por tipo de AI
    /// </summary>
    void SetDetectionParameters(float detectionRange, float fieldOfView, LayerMask obstacleLayerMask);
    
    /// <summary>
    /// MEJORA: Obtener configuración actual de detección
    /// </summary>
    DetectionConfig GetDetectionConfig();
    
    /// <summary>
    /// MEJORA: Detección auditiva para AI más avanzada
    /// </summary>
    bool CanHearPlayer(Transform player, float noiseLevel = 1f);
    
    /// <summary>
    /// MEJORA: Predicción de posición del player basada en movimiento
    /// Útil para aim prediction y pathfinding anticipativo
    /// </summary>
    Vector3 GetPredictedPlayerPosition(float predictionTime = 1f);
    
    /// <summary>
    /// MEJORA: Obtener el ángulo hacia el player para steering behaviors
    /// </summary>
    float GetAngleToPlayer(Transform player);
    
    /// <summary>
    /// MEJORA: Check si el player está en el campo de visión (sin obstáculos)
    /// Separado del line-of-sight para optimización
    /// </summary>
    bool IsPlayerInFieldOfView(Transform player);
    
    /// <summary>
    /// MEJORA: Invalidar cache manualmente para situaciones especiales
    /// </summary>
    void InvalidateCache();
    
    /// <summary>
    /// MEJORA: Obtener información de debug para visualización en editor
    /// </summary>
    (Vector3 position, float range, float fov, bool hasLOS) GetDebugInfo();

    public void SetPersonalityType(AIPersonalityType p_newPersonality);
    public DetectionResult GetCurrentDetectionResult();
}