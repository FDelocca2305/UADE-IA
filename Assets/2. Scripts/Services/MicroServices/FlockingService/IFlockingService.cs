using FlockingSystem;

namespace Services.MicroServices.FlockingService
{
    /// <summary>
    /// Service for managing flocking entities.
    /// Uses spatial hashing for efficient neighbor queries O(n) instead of O(n²).
    /// </summary>
    public interface IFlockingService : IGameService
    {
        /// <summary>
        /// Register a new flocking entity.
        /// </summary>
        void RegisterEntity(FlockingEntity p_entity);

        /// <summary>
        /// Unregister a flocking entity (called on destroy).
        /// </summary>
        void UnregisterEntity(FlockingEntity p_entity);

        /// <summary>
        /// Get all neighbors within a radius of an entity.
        /// Uses spatial hashing for O(n) performance.
        /// </summary>
        System.Collections.Generic.List<FlockingEntity> GetNeighbors(FlockingEntity p_entity, float p_radius);

        /// <summary>
        /// Get total number of registered entities.
        /// </summary>
        int GetEntityCount();

        /// <summary>
        /// Update spatial hash cell size at runtime.
        /// Recommended: 2x the average detection radius for optimal performance.
        /// </summary>
        void SetSpatialHashCellSize(float p_cellSize);
    }
}
