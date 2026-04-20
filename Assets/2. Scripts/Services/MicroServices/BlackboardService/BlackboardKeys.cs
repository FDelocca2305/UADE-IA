namespace Services.MicroServices.BlackboardService
{
    /// <summary>
    /// Centralized keys for the blackboard system to ensure consistency and prevent typos.
    /// 
    /// IMPROVEMENT: Organized by categories for better maintainability
    /// IMPROVEMENT: Added additional keys for advanced features NOT IMPLEMENTED YET
    /// IMPROVEMENT: Detailed documentation of the expected data type for each key
    /// </summary>
    public static class BlackboardKeys
    {
        #region Player Information
        /// <summary>Transform - The player's transform reference</summary>
        public const string PLAYER_TRANSFORM = "player_transform";
    
        /// <summary>Vector3 - Current player position</summary>
        public const string PLAYER_POSITION = "player_position";
    
        /// <summary>Vector3 - Last known player position when detected</summary>
        public const string PLAYER_LAST_SEEN = "player_last_seen";
    
        /// <summary>Vector3 - Last known player position for AI decision making</summary>
        public const string LAST_KNOWN_PLAYER_POSITION = "last_known_player_position";
    
        /// <summary>bool - Whether any AI has detected the player</summary>
        public const string PLAYER_DETECTED = "player_detected";
    
        /// <summary>float - Time when player was last seen</summary>
        public const string PLAYER_LAST_SEEN_TIME = "player_last_seen_time";
    
        /// <summary>Vector3 - Player's predicted position based on movement</summary>
        public const string PLAYER_PREDICTED_POSITION = "player_predicted_position";

        /// <summary>bool - Global alert state raised by civilians (minimum scope)</summary>
        public const string GLOBAL_ALERT = "global_alert";
        #endregion
    
        #region Alert System
        /// <summary>int - Current alert level (0=Normal, 1=Suspicious, 2=Alert, 3=Combat)</summary>
        public const string ALERT_LEVEL = "alert_level";
    
        /// <summary>Vector3 - Position where the alert was triggered</summary>
        public const string ALERT_POSITION = "alert_position";
    
        /// <summary>float - Time when alert was raised</summary>
        public const string ALERT_TIME = "alert_time";
    
        /// <summary>Transform - AI that triggered the last alert</summary>
        public const string LAST_ALERT_SOURCE = "last_alert_source";
    
        /// <summary>float - Duration of current alert state</summary>
        public const string ALERT_DURATION = "alert_duration";
        #endregion
    
        #region AI Coordination
        /// <summary>List&lt;Transform&gt; - List of guards currently chasing player</summary>
        public const string GUARDS_CHASING = "guards_chasing";
    
        /// <summary>List&lt;Transform&gt; - List of guards investigating areas</summary>
        public const string GUARDS_INVESTIGATING = "guards_investigating";
    
        /// <summary>List&lt;Vector3&gt; - Areas where civilians are panicking</summary>
        public const string CIVILIAN_PANIC_AREAS = "civilian_panic_areas";
    
        /// <summary>int - Number of active AI units</summary>
        public const string ACTIVE_AI_COUNT = "active_ai_count";
    
        /// <summary>Dictionary&lt;Transform, Vector3&gt; - Last known positions of each AI</summary>
        public const string AI_POSITIONS = "ai_positions";
        #endregion
    
        #region Game State
        /// <summary>GameState - Current game state from GameStateManager</summary>
        public const string GAME_STATE = "game_state";
    
        /// <summary>bool - Whether escape routes are blocked</summary>
        public const string ESCAPE_ROUTES_BLOCKED = "escape_routes_blocked";
    
        /// <summary>float - Game difficulty multiplier</summary>
        public const string DIFFICULTY_MULTIPLIER = "difficulty_multiplier";
    
        /// <summary>bool - Whether game is paused</summary>
        public const string GAME_PAUSED = "game_paused";
        #endregion
    
        #region Combat Information
        /// <summary>bool - Whether combat is currently active</summary>
        public const string COMBAT_ACTIVE = "combat_active";
    
        /// <summary>Vector3 - Last position where shots were fired</summary>
        public const string LAST_SHOT_POSITION = "last_shot_position";
    
        /// <summary>float - Time of last shot fired</summary>
        public const string LAST_SHOT_TIME = "last_shot_time";
    
        /// <summary>Transform - Who fired the last shot</summary>
        public const string LAST_SHOOTER = "last_shooter";
        #endregion
    
        #region Environmental
        /// <summary>List&lt;Vector3&gt; - Known safe positions for AI retreat</summary>
        public const string SAFE_POSITIONS = "safe_positions";
    
        /// <summary>List&lt;Vector3&gt; - Known dangerous areas to avoid</summary>
        public const string DANGEROUS_AREAS = "dangerous_areas";
    
        /// <summary>List&lt;Transform&gt; - Available cover points</summary>
        public const string COVER_POINTS = "cover_points";
    
        /// <summary>Bounds - Play area boundaries</summary>
        public const string PLAY_AREA_BOUNDS = "play_area_bounds";
        #endregion
    
        #region Performance Tracking
        /// <summary>int - Current frame number for cache invalidation</summary>
        public const string CURRENT_FRAME = "current_frame";
    
        /// <summary>float - AI update frequency modifier for performance</summary>
        public const string AI_UPDATE_FREQUENCY = "ai_update_frequency";
    
        /// <summary>bool - Whether AI debugging is enabled</summary>
        public const string AI_DEBUG_ENABLED = "ai_debug_enabled";
        #endregion
    
        #region Event System
        /// <summary>Queue&lt;AIEvent&gt; - Queue of AI events to process</summary>
        public const string AI_EVENT_QUEUE = "ai_event_queue";
    
        /// <summary>float - Time of last major AI event</summary>
        public const string LAST_AI_EVENT_TIME = "last_ai_event_time";
        #endregion
    }
}