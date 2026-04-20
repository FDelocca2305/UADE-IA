using System;
using Services;

namespace Services.MicroServices.BlackboardService
{
    /// <summary>
    /// Interface for the centralized AI memory system that allows NPCs to share information
    /// and coordinate their behavior without direct coupling.
    /// 
    /// MEJORA: Agregado soporte para callbacks tipados para evitar boxing/unboxing
    /// MEJORA: Agregado Clear() para mejor gestión de memoria en transiciones de nivel
    /// </summary>
    public interface IBlackboardService : IGameService
    {
        /// <summary>
        /// Gets a value from the blackboard with type safety
        /// </summary>
        T GetValue<T>(string key);
    
        /// <summary>
        /// Sets a value in the blackboard and notifies subscribers
        /// </summary>
        void SetValue<T>(string key, T value);
    
        /// <summary>
        /// Checks if a key exists in the blackboard
        /// </summary>
        bool HasKey(string key);
    
        /// <summary>
        /// Subscribe to changes of a specific key with typed callback
        /// MEJORA: Callback tipado para mejor performance y type safety
        /// </summary>
        void Subscribe<T>(string key, Action<T> callback);
    
        /// <summary>
        /// Legacy subscribe method for object-based callbacks (backward compatibility)
        /// </summary>
        void Subscribe(string key, Action<object> callback);
    
        /// <summary>
        /// Unsubscribe from key changes with typed callback
        /// </summary>
        void Unsubscribe<T>(string key, Action<T> callback);
    
        /// <summary>
        /// Legacy unsubscribe method for object-based callbacks
        /// </summary>
        void Unsubscribe(string key, Action<object> callback);
    
        /// <summary>
        /// MEJORA: Método para limpiar datos temporales y optimizar memoria
        /// Útil durante transiciones de nivel o reset del juego
        /// </summary>
        void Clear();
    
        /// <summary>
        /// MEJORA: Método para cleanup automático de datos temporales
        /// Debe ser llamado periódicamente por el AISystemInitializer
        /// </summary>
        void CleanupTemporaryData();
    }
}