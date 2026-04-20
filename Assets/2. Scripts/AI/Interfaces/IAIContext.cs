using Services.MicroServices.BlackboardService;
using UnityEngine;

/// <summary>
/// Provides complete context information that AI systems need to make decisions.
/// Acts as a facade to abstract complex subsystems and provide a clean API for AI logic.
/// 
/// MEJORA: Agregado cache para valores costosos de calcular (distancias, line of sight)
/// MEJORA: Integración con el sistema de UpdateManager existente para mejor performance
/// </summary>
public interface IAIContext
{
    /// <summary>
    /// Gets the transform of the AI entity that owns this context
    /// </summary>
    Transform GetTransform();
    
    /// <summary>
    /// Gets access to the shared blackboard system
    /// </summary>
    IBlackboardService GetBlackboard();
    
    /// <summary>
    /// Checks if the player is currently visible to this AI
    /// Cached para evitar raycast múltiples por frame
    /// </summary>
    bool IsPlayerVisible();
    
    /// <summary>
    /// Gets the current player position
    /// Con fallback a última posición conocida si player no está disponible
    /// </summary>
    Vector3 GetPlayerPosition();
    
    /// <summary>
    /// Gets the distance to the player
    /// MEJORA: Cached para evitar cálculos repetidos en el mismo frame
    /// </summary>
    float GetDistanceToPlayer();
    
    /// <summary>
    /// Gets the AI personality type for behavior customization
    /// </summary>
    AIPersonalityType GetPersonalityType();
    
    /// <summary>
    /// Soporte para múltiples targets, no solo el player
    /// Útil para escorts, grupos, etc.
    /// </summary>
    Transform GetTarget(string targetTag = "Player");
    
    /// <summary>
    /// Obtener distancia a cualquier target
    /// </summary>
    float GetDistanceToTarget(string targetTag = "Player");
    
    /// <summary>
    /// Check de visibilidad para cualquier target
    /// </summary>
    bool IsTargetVisible(string targetTag = "Player");
    
    /// <summary>
    /// Acceso directo al detector para casos avanzados
    /// </summary>
    IPlayerDetector GetPlayerDetector();
    
    /// <summary>
    /// Acceso al movement controller para casos avanzados
    /// </summary>
    IAIMovementController GetMovementController();
    
    /// <summary>
    /// Frame en el que se calcularon los valores cacheados
    /// Para invalidar cache automáticamente
    /// </summary>
    int GetCacheFrame();
    
    /// <summary>
    /// Forzar recálculo de valores cacheados
    /// </summary>
    void InvalidateCache();
}