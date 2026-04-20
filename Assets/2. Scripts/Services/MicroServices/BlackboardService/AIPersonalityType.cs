namespace Services.MicroServices.BlackboardService
{
    /// <summary>
    /// Defines different AI personality types that modify behavior patterns.
    /// Each personality affects decision making, reaction times, and coordination.
    /// 
    /// MEJORA: Agregados personality types más específicos para variety
    /// MEJORA: Cada type puede tener configuraciones asociadas via ScriptableObjects
    /// </summary>
    public enum AIPersonalityType
    {
        /// <summary>
        /// Aggressive guards that pursue immediately and coordinate attacks
        /// - React quickly to threats
        /// - Shorter investigation time
        /// - More likely to call for backup
        /// - Higher movement and attack speeds
        /// </summary>
        Aggressive,
    
        /// <summary>
        /// Conservative guards that are more cautious and methodical
        /// - Longer investigation periods
        /// - More thorough searches
        /// - Prefer group actions over solo pursuits
        /// - Better at maintaining formations
        /// </summary>
        Conservative,
    
        /// <summary>
        /// Cautious guards that balance aggression with careful observation
        /// - Medium reaction times
        /// - Investigate thoroughly before acting
        /// - Good balance between pursuit and caution
        /// - Standard coordination behavior
        /// </summary>
        Cautious,
    
        /// <summary>
        /// Civilian NPCs that panic and flee from danger
        /// - Non-hostile behavior
        /// - Create distractions when panicked
        /// - Can alert guards to player presence
        /// - Unpredictable movement when scared
        /// </summary>
        Civilian,
    
        /// <summary>
        /// Player controller personality (for AI systems that need to track player)
        /// - Used for prediction systems
        /// - Helps AI anticipate player behavior
        /// </summary>
        Player,
    
        /// <summary>
        /// MEJORA: Elite guards with advanced tactics and coordination
        /// - Longer detection range
        /// - Better aim and tactical movement
        /// - Can flank and use advanced formations
        /// - Communicate more effectively
        /// </summary>
        Elite,
    
        /// <summary>
        /// MEJORA: Patrol guards focused on area coverage
        /// - Stick to patrol routes unless alerted
        /// - Good at systematic searching
        /// - Less aggressive but more persistent
        /// - Report suspicious activity to others
        /// </summary>
        Patrol,
    
        /// <summary>
        /// MEJORA: Coward personality that avoids confrontation
        /// - Runs away from combat
        /// - Calls for help instead of fighting
        /// - Can be useful for escort missions or VIP protection
        /// </summary>
        Coward,
    
        /// <summary>
        /// MEJORA: Berserker personality for high-risk, high-reward gameplay
        /// - Ignores self-preservation
        /// - Extremely aggressive but tactically poor
        /// - Doesn't coordinate well with others
        /// - High damage but vulnerable to tactics
        /// </summary>
        Berserker,
    
        /// <summary>
        /// MEJORA: Support personality for healers, buffers, or specialists
        /// - Stays behind other units
        /// - Focuses on supporting allies
        /// - Retreats when isolated
        /// - Can provide tactical advantages to groups
        /// </summary>
        Support
    }

    /// <summary>
    /// MEJORA: Extension methods para AIPersonalityType para configuración fácil
    /// </summary>
    public static class AIPersonalityExtensions
    {
        /// <summary>
        /// Gets the detection range multiplier for this personality type
        /// </summary>
        public static float GetDetectionRangeMultiplier(this AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive => 1.2f,
                AIPersonalityType.Conservative => 0.9f,
                AIPersonalityType.Cautious => 1.0f,
                AIPersonalityType.Elite => 1.5f,
                AIPersonalityType.Patrol => 1.0f,
                AIPersonalityType.Coward => 1.3f, // Cowards notice threats from farther
                AIPersonalityType.Berserker => 0.8f, // Too focused to notice subtleties
                AIPersonalityType.Support => 1.1f,
                AIPersonalityType.Civilian => 0.7f,
                AIPersonalityType.Player => 1.0f,
                _ => 1.0f
            };
        }
    
        /// <summary>
        /// Gets the movement speed multiplier for this personality type
        /// </summary>
        public static float GetMovementSpeedMultiplier(this AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive => 1.1f,
                AIPersonalityType.Conservative => 0.9f,
                AIPersonalityType.Cautious => 1.0f,
                AIPersonalityType.Elite => 1.0f, // Tactical, not rushed
                AIPersonalityType.Patrol => 0.8f,
                AIPersonalityType.Coward => 1.4f, // Fast when fleeing
                AIPersonalityType.Berserker => 1.3f,
                AIPersonalityType.Support => 0.7f,
                AIPersonalityType.Civilian => 1.2f, // Can run fast when panicked
                AIPersonalityType.Player => 1.0f,
                _ => 1.0f
            };
        }
    
        /// <summary>
        /// Gets the reaction time multiplier (lower = faster reactions)
        /// </summary>
        public static float GetReactionTimeMultiplier(this AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive => 0.7f,
                AIPersonalityType.Conservative => 1.3f,
                AIPersonalityType.Cautious => 1.0f,
                AIPersonalityType.Elite => 0.5f,
                AIPersonalityType.Patrol => 1.0f,
                AIPersonalityType.Coward => 0.6f, // Quick to notice danger
                AIPersonalityType.Berserker => 0.4f,
                AIPersonalityType.Support => 1.1f,
                AIPersonalityType.Civilian => 1.5f,
                AIPersonalityType.Player => 1.0f,
                _ => 1.0f
            };
        }
    
        /// <summary>
        /// Checks if this personality type is hostile to the player
        /// </summary>
        public static bool IsHostile(this AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive => true,
                AIPersonalityType.Conservative => true,
                AIPersonalityType.Cautious => true,
                AIPersonalityType.Elite => true,
                AIPersonalityType.Patrol => true,
                AIPersonalityType.Berserker => true,
                AIPersonalityType.Support => true,
                AIPersonalityType.Coward => false,
                AIPersonalityType.Civilian => false,
                AIPersonalityType.Player => false,
                _ => false
            };
        }
    }
}