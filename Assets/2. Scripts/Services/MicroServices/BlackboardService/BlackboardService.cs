using System;
using System.Collections.Generic;
using System.Linq;
using Services.MicroServices.GameStateService;
using Services.MicroServices.UpdateService;
using UnityEngine;

namespace Services.MicroServices.BlackboardService
{
    /// <summary>
    /// Central AI memory system that allows NPCs to share information and coordinate behavior.
    /// Integrates with the existing ServiceLocator pattern for consistent architecture.
    /// 
    /// IMPROVEMENT: Implemented as IGameService
    /// IMPROVEMENT: Added typed callback system for better performance
    /// IMPROVEMENT: Smart cache to avoid unnecessary boxing/unboxing
    /// IMPROVEMENT: Automatic cleanup of temporary data for better memory management
    /// IMPROVEMENT: Integration with UpdateManager for periodic cleanup
    /// </summary>
    public class BlackboardService : IBlackboardService, IUpdateListener
    {
        private BlackboardServiceSettings m_settings;
        // Core data storage
        private Dictionary<string, object> m_data;
    
        // Dual callback system - typed and object-based for flexibility
        private Dictionary<string, List<Action<object>>> m_objectSubscribers;
        private Dictionary<string, object> m_typedSubscribers; // Will store List<Action<T>>
    
        // Performance tracking
        private float m_lastCleanupTime;
        private int m_frameCounter;
    
        private readonly IUpdateService m_updateService;
        private readonly IGameStateService m_gameStateService;

        public BlackboardService(IUpdateService p_updateService, IGameStateService p_gameStateService)
        {
            m_updateService = p_updateService;
            m_gameStateService = p_gameStateService;
        }

        ~BlackboardService()
        {
            UnsubscribeUpdateService();
        }
    
        public void Initialize()
        {
            m_settings = MyGame.BlackboardServiceSettings;
        
            m_data = new Dictionary<string, object>();
            m_objectSubscribers = new Dictionary<string, List<Action<object>>>();
            m_typedSubscribers = new Dictionary<string, object>();
        
            SubscribeUpdateService();
        
            InitializeDefaultValues();
        }
    
        #region Core Blackboard Functionality
    
        public T GetValue<T>(string p_key)
        {
            if (m_data.TryGetValue(p_key, out object l_value))
            {
                try
                {
                    return (T)l_value;
                }
                catch (InvalidCastException)
                {
                    MyLogger.LogError($"Blackboard: Failed to cast '{p_key}' from {l_value?.GetType().Name} to {typeof(T).Name}");
                    return default(T);
                }
            }
        
            return default(T);
        }
    
        public void SetValue<T>(string p_key, T p_value)
        {
            object l_oldValue = m_data.ContainsKey(p_key) ? m_data[p_key] : null;
            m_data[p_key] = p_value;
        
            // Notify all subscribers
            NotifyObjectSubscribers(p_key, p_value);
            NotifyTypedSubscribers<T>(p_key, p_value);
        }
    
        /// <summary>
        /// Check if a key is frequently updated to avoid debug spam
        /// </summary>
        private bool IsFrequentlyUpdatedKey(string p_key)
        {
            // Keys that are updated frequently and shouldn't spam the console
            return p_key.Equals(BlackboardKeys.CURRENT_FRAME) ||
                   p_key.Equals(BlackboardKeys.PLAYER_TRANSFORM) ||
                   p_key.Equals(BlackboardKeys.PLAYER_POSITION) ||
                   p_key.Equals(BlackboardKeys.PLAYER_PREDICTED_POSITION) ||
                   p_key.Equals(BlackboardKeys.LAST_KNOWN_PLAYER_POSITION) ||
                   p_key.Equals(BlackboardKeys.PLAYER_LAST_SEEN) ||
                   p_key.Equals(BlackboardKeys.PLAYER_LAST_SEEN_TIME) ||
                   p_key.Equals(BlackboardKeys.LAST_SHOT_TIME) ||
                   p_key.Equals(BlackboardKeys.ALERT_TIME) ||
                   p_key.Contains("_DetectionLevel") ||
                   p_key.Contains("_CanSeePlayer") ||
                   p_key.Contains("_CurrentPosition") ||
                   p_key.Contains("_LastUpdateTime") ||
                   p_key.Contains("_LastShootTime") ||
                   p_key.Contains("_Time") ||
                   p_key.EndsWith("_FRAME_DATA") ||
                   p_key.EndsWith("_TIME") ||
                   (p_key.StartsWith("Guard_") && (p_key.Contains("_DetectionLevel") || p_key.Contains("_CanSeePlayer")));
        }    public bool HasKey(string p_key)
        {
            return m_data.ContainsKey(p_key);
        }
    
        #endregion
    
        #region Typed Subscription System
    
        /// <summary>
        /// Subscribe with typed callback for better performance and type safety
        /// </summary>
        public void Subscribe<T>(string p_key, Action<T> p_callback)
        {
            string l_typedKey = GetTypedKey<T>(p_key);
        
            if (!m_typedSubscribers.ContainsKey(l_typedKey))
                m_typedSubscribers[l_typedKey] = new List<Action<T>>();
        
            var l_callbacks = (List<Action<T>>)m_typedSubscribers[l_typedKey];
            l_callbacks.Add(p_callback);
        }
    
        /// <summary>
        /// Legacy object-based subscription for backward compatibility
        /// </summary>
        public void Subscribe(string p_key, Action<object> p_callback)
        {
            if (!m_objectSubscribers.ContainsKey(p_key))
                m_objectSubscribers[p_key] = new List<Action<object>>();
        
            m_objectSubscribers[p_key].Add(p_callback);
        }
    
        public void Unsubscribe<T>(string p_key, Action<T> p_callback)
        {
            string l_typedKey = GetTypedKey<T>(p_key);
        
            if (m_typedSubscribers.TryGetValue(l_typedKey, out object l_callbacksObj))
            {
                var l_callbacks = (List<Action<T>>)l_callbacksObj;
                l_callbacks.Remove(p_callback);
            
                if (l_callbacks.Count == 0)
                    m_typedSubscribers.Remove(l_typedKey);
            }
        }
    
        public void Unsubscribe(string p_key, Action<object> p_callback)
        {
            if (m_objectSubscribers.TryGetValue(p_key, out var l_callbacks))
            {
                l_callbacks.Remove(p_callback);
            
                if (l_callbacks.Count == 0)
                    m_objectSubscribers.Remove(p_key);
            }
        }
    
        #endregion

        #region Minimum Scope Support

        /// <summary>
        /// TODO(MIN_SCOPE): Conditional setter for non-minimum features
        /// </summary>
        private void SetValueIfMinScope<T>(string p_key, T p_value, bool p_isMinimumKey = false)
        {
            if (m_settings.UseMinimumScope && !p_isMinimumKey && !m_settings.EnableAdvancedFeatures)
            {
                // TODO(MIN_SCOPE): Feature parked - key: {key}
                return;
            }
            SetValue(p_key, p_value);
        }

        /// <summary>
        /// TODO(MIN_SCOPE): Check if key is part of minimum scope
        /// </summary>
        private bool IsMinimumScopeKey(string p_key)
        {
            return p_key == BlackboardKeys.PLAYER_TRANSFORM ||
                   p_key == BlackboardKeys.LAST_KNOWN_PLAYER_POSITION ||
                   p_key == BlackboardKeys.GLOBAL_ALERT;
        }

        #endregion

        #region Initialization and Cleanup
    
        private void InitializeDefaultValues()
        {
            // Player information
            SetValue(BlackboardKeys.PLAYER_DETECTED, false);
            SetValue(BlackboardKeys.PLAYER_LAST_SEEN_TIME, 0f);

            // Minimum scope keys (always initialized)
            SetValue(BlackboardKeys.GLOBAL_ALERT, false);
        
            // Alert system
            SetValue(BlackboardKeys.ALERT_LEVEL, 0);
            SetValue(BlackboardKeys.ALERT_DURATION, 0f);
        
            // AI coordination
            SetValue(BlackboardKeys.GUARDS_CHASING, new List<Transform>());
            SetValue(BlackboardKeys.GUARDS_INVESTIGATING, new List<Transform>());
            SetValue(BlackboardKeys.CIVILIAN_PANIC_AREAS, new List<Vector3>());
            SetValue(BlackboardKeys.ACTIVE_AI_COUNT, 0);
            SetValue(BlackboardKeys.AI_POSITIONS, new Dictionary<Transform, Vector3>());
        
            // Game state integration
            SetValue(BlackboardKeys.GAME_STATE, m_gameStateService.GetCurrentState());
            // Subscribe to game state changes
            m_gameStateService.OnStateChanged += GameStateServiceOnOnStateChanged;
        
            // Combat
            SetValue(BlackboardKeys.COMBAT_ACTIVE, false);
            SetValue(BlackboardKeys.LAST_SHOT_TIME, 0f);
        
            // Environmental
            SetValue(BlackboardKeys.SAFE_POSITIONS, new List<Vector3>());
            SetValue(BlackboardKeys.DANGEROUS_AREAS, new List<Vector3>());
            SetValue(BlackboardKeys.COVER_POINTS, new List<Transform>());
        
            // Performance
            SetValue(BlackboardKeys.CURRENT_FRAME, 0);
            SetValue(BlackboardKeys.AI_UPDATE_FREQUENCY, 1.0f);
            SetValue(BlackboardKeys.DIFFICULTY_MULTIPLIER, 1.0f);
        }

        private void GameStateServiceOnOnStateChanged(GameState p_previousState, GameState p_newState)
        {
            SetValue(BlackboardKeys.GAME_STATE, p_newState);
        }

        public void CleanupTemporaryData()
        {
            // Clean panic areas
            var l_panicAreas = GetValue<List<Vector3>>(BlackboardKeys.CIVILIAN_PANIC_AREAS);
            if (l_panicAreas != null && l_panicAreas.Count > m_settings.MaxPanicAreas)
            {
                l_panicAreas.RemoveRange(0, l_panicAreas.Count - m_settings.MaxPanicAreas);
                SetValue(BlackboardKeys.CIVILIAN_PANIC_AREAS, l_panicAreas);
            }
        
            // Clean AI positions of destroyed objects
            var l_aiPositions = GetValue<Dictionary<Transform, Vector3>>(BlackboardKeys.AI_POSITIONS);
            if (l_aiPositions != null)
            {
                var l_keysToRemove = l_aiPositions.Keys.Where(p_k => p_k == null).ToList();
                foreach (var l_key in l_keysToRemove)
                {
                    l_aiPositions.Remove(l_key);
                }
                if (l_keysToRemove.Count > 0)
                    SetValue(BlackboardKeys.AI_POSITIONS, l_aiPositions);
            }
        
            // Clean chasing guards list
            var l_chasingGuards = GetValue<List<Transform>>(BlackboardKeys.GUARDS_CHASING);
            if (l_chasingGuards != null)
            {
                int l_originalCount = l_chasingGuards.Count;
                l_chasingGuards.RemoveAll(p_g => p_g == null);
                if (l_chasingGuards.Count != l_originalCount)
                    SetValue(BlackboardKeys.GUARDS_CHASING, l_chasingGuards);
            }
        
            // Clean investigating guards list
            var l_investigatingGuards = GetValue<List<Transform>>(BlackboardKeys.GUARDS_INVESTIGATING);
            if (l_investigatingGuards != null)
            {
                int l_originalCount = l_investigatingGuards.Count;
                l_investigatingGuards.RemoveAll(p_g => p_g == null);
                if (l_investigatingGuards.Count != l_originalCount)
                    SetValue(BlackboardKeys.GUARDS_INVESTIGATING, l_investigatingGuards);
            }
        }
    
        public void Clear()
        {
            m_data.Clear();
            m_objectSubscribers.Clear();
            m_typedSubscribers.Clear();
        
            InitializeDefaultValues();
        }
    
        #endregion
    
        #region Helper Methods
    
        private void NotifyObjectSubscribers(string p_key, object p_value)
        {
            if (m_objectSubscribers.TryGetValue(p_key, out var l_callbacks))
            {
                // ToList() prevents modification during iteration
                foreach (var l_callback in l_callbacks.ToList())
                {
                    try
                    {
                        l_callback?.Invoke(p_value);
                    }
                    catch (Exception l_e)
                    {
                        MyLogger.LogError($"Blackboard: Error notifying object subscriber for '{p_key}': {l_e.Message}");
                    }
                }
            }
        }
    
        private void NotifyTypedSubscribers<T>(string p_key, T p_value)
        {
            string l_typedKey = GetTypedKey<T>(p_key);
        
            if (m_typedSubscribers.TryGetValue(l_typedKey, out object l_callbacksObj))
            {
                var l_callbacks = (List<Action<T>>)l_callbacksObj;
            
                foreach (var l_callback in l_callbacks.ToList())
                {
                    try
                    {
                        l_callback?.Invoke(p_value);
                    }
                    catch (Exception l_e)
                    {
                        MyLogger.LogError($"Blackboard: Error notifying typed subscriber for '{p_key}': {l_e.Message}");
                    }
                }
            }
        }
    
        private string GetTypedKey<T>(string p_key)
        {
            return $"{p_key}_{typeof(T).Name}";
        }
    
        #endregion

        public void MyUpdate()
        {
            m_frameCounter++;
            SetValue(BlackboardKeys.CURRENT_FRAME, m_frameCounter);
        
            // Periodic cleanup
            if (Time.time - m_lastCleanupTime >= m_settings.CleanupInterval)
            {
                CleanupTemporaryData();
                m_lastCleanupTime = Time.time;
            }
        }

        public void SubscribeUpdateService()
        {
            m_updateService.AddUpdateListener(this);
        }

        public void UnsubscribeUpdateService()
        {
            m_updateService.RemoveUpdateListener(this);
        }
    }
}