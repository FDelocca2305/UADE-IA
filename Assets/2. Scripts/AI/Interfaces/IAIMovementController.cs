using UnityEngine;

/// <summary>
/// Interface for AI movement and steering behaviors.
/// Provides high-level movement commands that abstract the underlying movement implementation.
/// 
/// MEJORA: Agregado soporte para different movement modes (walk, run, sneak)
/// MEJORA: Callbacks para notificar cuando se completan movimientos
/// MEJORA: Integración con obstacle avoidance y pathfinding
/// MEJORA: Soporte para movement constraints (can't leave patrol area, etc.)
/// </summary>
public interface IAIMovementController
{
    /// <summary>
    /// Move towards a specific target position
    /// </summary>
    /// <param name="target">Target position to move to</param>
    /// <param name="speed">Movement speed modifier</param>
    void MoveTo(Vector3 target, float speed);
    
    /// <summary>
    /// Flee from a specific position
    /// </summary>
    /// <param name="fromPosition">Position to flee from</param>
    /// <param name="speed">Movement speed modifier</param>
    void Flee(Vector3 fromPosition, float speed);
    
    /// <summary>
    /// Start patrolling between waypoints
    /// </summary>
    /// <param name="waypoints">Array of patrol points</param>
    /// <param name="speed">Patrol speed</param>
    void Patrol(Transform[] waypoints, float speed);
    
    /// <summary>
    /// Stop all movement
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Check if AI has reached its current destination
    /// </summary>
    /// <returns>True if destination reached</returns>
    bool HasReachedDestination();
    
    /// <summary>
    /// Set a target to continuously follow (for chase behavior)
    /// </summary>
    /// <param name="target">Target to follow</param>
    void SetSteeringTarget(Transform target);
    
    /// <summary>
    /// MEJORA: Diferentes modos de movimiento para variety en comportamiento
    /// </summary>
    void SetMovementMode(MovementMode mode);
    
    /// <summary>
    /// MEJORA: Callback cuando se completa un movimiento
    /// Útil para state machines que esperan completar movimientos
    /// </summary>
    System.Action OnMovementComplete { get; set; }
    
    /// <summary>
    /// MEJORA: Callback cuando el movimiento es bloqueado por obstáculos
    /// </summary>
    System.Action OnMovementBlocked { get; set; }
    
    /// <summary>
    /// MEJORA: Set constraints para limitar área de movimiento
    /// Útil para guards que no deben salir de su zona
    /// </summary>
    void SetMovementConstraints(Bounds allowedArea);
    
    /// <summary>
    /// MEJORA: Remove movement constraints
    /// </summary>
    void ClearMovementConstraints();
    
    /// <summary>
    /// MEJORA: Get current movement status para debug y AI logic
    /// </summary>
    MovementStatus GetMovementStatus();
    
    /// <summary>
    /// MEJORA: Get current destination para debug
    /// </summary>
    Vector3 GetCurrentDestination();
    
    /// <summary>
    /// MEJORA: Rotar hacia una dirección sin moverse
    /// Útil para aiming y look-at behaviors
    /// </summary>
    void FaceDirection(Vector3 direction, float rotationSpeed = -1f);
    
    /// <summary>
    /// MEJORA: Obtener la velocidad actual para animaciones
    /// </summary>
    Vector3 GetCurrentVelocity();
    
    /// <summary>
    /// MEJORA: Pause/resume movement sin cambiar el target
    /// </summary>
    void PauseMovement();
    void ResumeMovement();
    bool IsMovementPaused { get; }
}

/// <summary>
/// MEJORA: Enum para diferentes modos de movimiento
/// </summary>
public enum MovementMode
{
    Walk,       // Velocidad normal
    Run,        // Velocidad alta (persecución)
    Sneak,      // Velocidad baja (patrulla sigilosa)
    Sprint      // Velocidad máxima (escape)
}

/// <summary>
/// MEJORA: Enum para estado del movimiento
/// </summary>
public enum MovementStatus
{
    Idle,           // No se está moviendo
    Moving,         // Moviéndose hacia target
    Patrolling,     // En modo patrulla
    Following,      // Siguiendo un target
    Fleeing,        // Huyendo
    Blocked,        // Movimiento bloqueado por obstáculo
    Constrained     // Movimiento limitado por constraints
}