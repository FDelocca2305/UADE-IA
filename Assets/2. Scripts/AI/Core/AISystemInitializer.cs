using Services.MicroServices.BlackboardService;
using Services;
using Services.MicroServices.GameStateService;
using UnityEngine;

/// <summary>
/// Initializes and manages the AI system lifecycle.
/// Integrates seamlessly with the existing GameManager and ServiceLocator architecture.
/// 
/// MEJORA: Implementado como BaseManager para consistencia con la arquitectura existente
/// MEJORA: Inicialización automática del player reference desde el tag
/// MEJORA: Manejo robusto de errores durante inicialización
/// MEJORA: Integración con el sistema de logging existente
/// MEJORA: Support para diferentes modos de inicialización (auto vs manual)
/// </summary>
public class AISystemInitializer : BaseManager
{
    [Header("AI System Configuration")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool enableAIDebugging = false;
    [SerializeField] private float aiUpdateFrequency = 1.0f;
    
    [Header("Player Detection")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool subscribeToPlayerMovement = true;
    
    private static IBlackboardService BlackboardService => ServiceLocator.Get<IBlackboardService>();
    private static IGameStateService GameStateService => ServiceLocator.Get<IGameStateService>();
    private Transform cachedPlayerTransform;
    
    protected override void OnInitialize()
    {
        MyLogger.LogInfo("AISystemInitializer: Starting AI system initialization...");
        
        try
        {
            InitializePlayerReference();
            ConfigureAISystem();
            
            MyLogger.LogInfo("AISystemInitializer: AI system initialized successfully");
        }
        catch (System.Exception e)
        {
            MyLogger.LogError($"AISystemInitializer: Failed to initialize AI system: {e.Message}");
        }
    }
    
    protected override void OnShutdown()
    {
        MyLogger.LogInfo("AISystemInitializer: Shutting down AI system...");
        
        // Blackboard will shutdown itself as it's a service
        // Just clean up local references
        cachedPlayerTransform = null;
        
        MyLogger.LogInfo("AISystemInitializer: AI system shutdown completed");
    }
    
    private void InitializePlayerReference()
    {
        // Try assigned reference first
        if (playerTransform != null)
        {
            cachedPlayerTransform = playerTransform;
            MyLogger.LogInfo($"AISystemInitializer: Using assigned player reference: {playerTransform.name}");
        }
        // MEJORA: Auto-find player by tag if enabled
        else if (autoFindPlayer)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                cachedPlayerTransform = playerObject.transform;
                MyLogger.LogInfo($"AISystemInitializer: Found player by tag '{playerTag}': {playerObject.name}");
            }
            else
            {
                MyLogger.LogWarning($"AISystemInitializer: Player not found with tag '{playerTag}'");
            }
        }
        
        // Register player in blackboard if found
        if (cachedPlayerTransform != null)
        {
            BlackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, cachedPlayerTransform);
            BlackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, cachedPlayerTransform.position);
            
            // MEJORA: Subscribe to player movement for real-time position updates
            if (subscribeToPlayerMovement)
            {
                StartCoroutine(UpdatePlayerPositionCoroutine());
            }
        }
    }
    
    private void ConfigureAISystem()
    {
        // Set initial AI configuration
        BlackboardService.SetValue(BlackboardKeys.AI_DEBUG_ENABLED, enableAIDebugging);
        BlackboardService.SetValue(BlackboardKeys.AI_UPDATE_FREQUENCY, aiUpdateFrequency);
        BlackboardService.SetValue(BlackboardKeys.ACTIVE_AI_COUNT, 0);
        
        // MEJORA: Get game state from existing GameStateManager
        BlackboardService.SetValue(BlackboardKeys.GAME_STATE, GameStateService.GetCurrentState());
        BlackboardService.SetValue(BlackboardKeys.GAME_PAUSED, GameStateService.GetCurrentState() == GameState.Paused);
        
        // Subscribe to game state changes
        GameStateService.OnStateChanged += OnGameStateChanged;
        MyLogger.LogInfo("AISystemInitializer: Subscribed to GameStateManager events");
        
        // MEJORA: Set up play area bounds from LevelManager if available
        if (LevelManager.Instance)
        {
            // You can extend LevelManager to provide bounds information
            // For now, set reasonable defaults
            BlackboardService.SetValue(BlackboardKeys.PLAY_AREA_BOUNDS, new Bounds(Vector3.zero, Vector3.one * 100f));
        }
        
        MyLogger.LogInfo("AISystemInitializer: AI system configuration completed");
    }
    
    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        BlackboardService.SetValue(BlackboardKeys.GAME_STATE, newState);
        BlackboardService.SetValue(BlackboardKeys.GAME_PAUSED, newState == GameState.Paused);
        
        // MEJORA: Reset AI states when game restarts
        if (newState == GameState.Playing && previousState is GameState.Menu or GameState.GameOver)
        {
            ResetAISystem();
        }
        
        MyLogger.LogDebug($"AISystemInitializer: Game state changed to {newState}");
    }
    
    private void ResetAISystem()
    {
        // Reset AI coordination states
        BlackboardService.SetValue(BlackboardKeys.ALERT_LEVEL, 0);
        BlackboardService.SetValue(BlackboardKeys.PLAYER_DETECTED, false);
        BlackboardService.SetValue(BlackboardKeys.COMBAT_ACTIVE, false);
        BlackboardService.SetValue(BlackboardKeys.GUARDS_CHASING, new System.Collections.Generic.List<Transform>());
        BlackboardService.SetValue(BlackboardKeys.GUARDS_INVESTIGATING, new System.Collections.Generic.List<Transform>());
        
        // Clean up temporary data
        BlackboardService.CleanupTemporaryData();
        
        MyLogger.LogInfo("AISystemInitializer: AI system reset for new game");
    }
    
    private System.Collections.IEnumerator UpdatePlayerPositionCoroutine()
    {
        while (cachedPlayerTransform)
        {
            BlackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, cachedPlayerTransform.position);
            
            // Update at the configured frequency
            yield return new WaitForSeconds(1f / aiUpdateFrequency);
        }
    }
    
    #region Public API for Runtime Configuration
    
    /// <summary>
    /// MEJORA: Allow runtime player assignment for dynamic scenarios
    /// </summary>
    public void SetPlayer(Transform newPlayer)
    {
        cachedPlayerTransform = newPlayer;
        playerTransform = newPlayer;
        
        BlackboardService.SetValue(BlackboardKeys.PLAYER_TRANSFORM, newPlayer);
        if (newPlayer != null)
        {
            BlackboardService.SetValue(BlackboardKeys.PLAYER_POSITION, newPlayer.position);
        }
        
        MyLogger.LogInfo($"AISystemInitializer: Player reference updated to {newPlayer?.name ?? "null"}");
    }
    
    /// <summary>
    /// MEJORA: Runtime configuration of AI parameters
    /// </summary>
    public void SetAIUpdateFrequency(float frequency)
    {
        aiUpdateFrequency = Mathf.Clamp(frequency, 0.1f, 60f);
        
        BlackboardService.SetValue(BlackboardKeys.AI_UPDATE_FREQUENCY, aiUpdateFrequency);
        
        MyLogger.LogInfo($"AISystemInitializer: AI update frequency set to {aiUpdateFrequency}");
    }
    
    /// <summary>
    /// MEJORA: Toggle AI debugging at runtime
    /// </summary>
    public void SetAIDebugging(bool enabled)
    {
        enableAIDebugging = enabled;
        
        BlackboardService.SetValue(BlackboardKeys.AI_DEBUG_ENABLED, enabled);
        
        MyLogger.LogInfo($"AISystemInitializer: AI debugging {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// MEJORA: Get current AI system status for monitoring
    /// </summary>
    public bool IsAISystemReady()
    {
        return BlackboardService.HasKey(BlackboardKeys.PLAYER_TRANSFORM) && IsInitialized;
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Reinitialize AI System")]
    private void ReinitializeAISystem()
    {
        if (IsInitialized)
        {
            Shutdown();
        }
        Initialize();
    }
    
    [ContextMenu("Print AI System Status")]
    private void PrintAISystemStatus()
    {
        MyLogger.LogInfo("=== AI SYSTEM STATUS ===");
        MyLogger.LogInfo($"Initialized: {IsInitialized}");
        MyLogger.LogInfo($"Player Found: {cachedPlayerTransform != null}");
        MyLogger.LogInfo($"AI Debugging: {enableAIDebugging}");
        MyLogger.LogInfo($"Update Frequency: {aiUpdateFrequency}");
        MyLogger.LogInfo("=======================");
    }
    
    #endregion
}