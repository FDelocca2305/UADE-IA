using UnityEngine;

/// <summary>
/// Simple test utility to validate Civilian Decision Tree functionality
/// </summary>
public class CivilianDecisionTreeTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runContinuousTest = false;
    [SerializeField] private float testInterval = 2f;
    
    private Civilian civilian;
    private CivilianDecisionTreeRunner dtRunner;
    private float lastTestTime;

    private void Start()
    {
        civilian = GetComponent<Civilian>();
        dtRunner = GetComponent<CivilianDecisionTreeRunner>();
        
        if (civilian == null)
        {
            Logger.LogError("CivilianDecisionTreeTest: No Civilian component found!");
            enabled = false;
            return;
        }
        
        Logger.LogInfo("CivilianDecisionTreeTest: Test initialized");
    }

    private void Update()
    {
        if (runContinuousTest && Time.time - lastTestTime > testInterval)
        {
            RunTest();
            lastTestTime = Time.time;
        }
    }

    [ContextMenu("Run Decision Tree Test")]
    public void RunTest()
    {
        if (civilian == null) return;

        MyLogger.LogInfo("=== CIVILIAN DECISION TREE TEST ===");
        
        // Test civilian state
        MyLogger.LogInfo($"Civilian Active: {civilian.IsActive}");
        MyLogger.LogInfo($"Can See Player: {civilian.HasLoS()}");
        MyLogger.LogInfo($"Distance to Player: {civilian.GetDistanceToPlayer():F2}");
        MyLogger.LogInfo($"Can Attack: {civilian.CanAttack}");
        MyLogger.LogInfo($"Using Decision Tree: {civilian.UseDecisionTree}");
        MyLogger.LogInfo($"Decision Tree Active: {civilian.IsDecisionTreeActive()}");
        
        // Test decision tree runner
        if (dtRunner != null)
        {
            MyLogger.LogInfo($"DT Debug Enabled: {dtRunner.DebugEnabled}");
            MyLogger.LogInfo($"DT Current Suggestion: {dtRunner.CurrentSuggestion}");
            MyLogger.LogInfo($"DT Last Suggestion: {dtRunner.LastSuggestion}");
            MyLogger.LogInfo($"DT Status: {dtRunner.GetStatus()}");
            
            // Manually trigger evaluation
            dtRunner.EvaluateDecisionTree();
        }
        else
        {
            MyLogger.LogInfo("Decision Tree Runner: Not found");
        }
        
        // Test blackboard
        var blackboard = ServiceLocator.Get<IBlackboard>();
        if (blackboard != null)
        {
            MyLogger.LogInfo($"Global Alert: {blackboard.GetValue<bool>(BlackboardKeys.GLOBAL_ALERT)}");
        }
        else
        {
            MyLogger.LogInfo("Blackboard: Not available");
        }
        
        MyLogger.LogInfo("=================================");
    }

    [ContextMenu("Toggle Global Alert")]
    public void ToggleGlobalAlert()
    {
        var blackboard = ServiceLocator.Get<IBlackboard>();
        if (blackboard != null)
        {
            bool currentAlert = blackboard.GetValue<bool>(BlackboardKeys.GLOBAL_ALERT);
            blackboard.SetValue(BlackboardKeys.GLOBAL_ALERT, !currentAlert);
            MyLogger.LogInfo($"Global Alert toggled to: {!currentAlert}");
        }
    }

    [ContextMenu("Force DT Evaluation")]
    public void ForceDTEvaluation()
    {
        if (dtRunner != null)
        {
            MyLogger.LogInfo("Forcing Decision Tree evaluation...");
            dtRunner.EvaluateDecisionTree();
        }
    }

    [ContextMenu("Test State Change - Flee")]
    public void TestStateChangeFlee()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Testing state change to Fleeing...");
            civilian.RequestStateChange("Fleeing");
        }
    }

    [ContextMenu("Test State Change - Idle")]
    public void TestStateChangeIdle()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Testing state change to Idle...");
            civilian.RequestStateChange("Idle");
        }
    }

    [ContextMenu("Test State Change - Pursue")]
    public void TestStateChangePursue()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Testing state change to Pursue...");
            civilian.RequestStateChange("Pursue");
        }
    }

    [ContextMenu("Test State Change - Evade")]
    public void TestStateChangeEvade()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Testing state change to Evade...");
            civilian.RequestStateChange("Evade");
        }
    }

    [ContextMenu("Test State Change - Attack")]
    public void TestStateChangeAttack()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Testing state change to Attack...");
            civilian.RequestStateChange("Attack");
        }
    }

    [ContextMenu("List Available FSM States")]
    public void ListAvailableFSMStates()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("Listing available FSM states...");
            civilian.DebugCivilianStatus(); // This will show available states
        }
    }

    [ContextMenu("Check DT vs FSM State Sync")]
    public void CheckDTvsFSMStateSync()
    {
        if (dtRunner != null)
        {
            MyLogger.LogInfo("Checking DT vs FSM state synchronization...");
            dtRunner.DebugDecisionTreeStatus(); // This will show state matching info
        }
    }

    [ContextMenu("Test Attack Cycle Completion")]
    public void TestAttackCycleCompletion()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("=== TESTING ATTACK CYCLE COMPLETION ===");
            civilian.OnAttackCycleComplete();
            MyLogger.LogInfo("Attack cycle completion test finished");
        }
    }

    [ContextMenu("Test Melee Damage Event")]
    public void TestMeleeDamageEvent()
    {
        if (civilian != null)
        {
            MyLogger.LogInfo("=== TESTING MELEE DAMAGE EVENT ===");
            civilian.DealMeleeAttack();
            MyLogger.LogInfo("Melee damage event test finished");
        }
    }

    [ContextMenu("Force DT Re-evaluation")]
    public void ForceDTReevaluation()
    {
        if (dtRunner != null)
        {
            MyLogger.LogInfo("=== FORCING DT RE-EVALUATION ===");
            dtRunner.EvaluateDecisionTree();
            MyLogger.LogInfo("DT re-evaluation completed");
        }
    }

    [ContextMenu("Test Enhanced Status Logging")]
    public void TestEnhancedStatusLogging()
    {
        MyLogger.LogInfo("=== ENHANCED STATUS TEST ===");
        
        if (civilian != null)
        {
            MyLogger.LogInfo($"Has LoS: {civilian.HasLoS()}");
            MyLogger.LogInfo($"In Melee Range: {civilian.IsPlayerInMeleeRange()}");
            MyLogger.LogInfo($"Distance: {civilian.GetDistanceToPlayer():F2}");
            MyLogger.LogInfo($"Can Attack: {civilian.CanAttack}");
        }

        if (dtRunner != null)
        {
            MyLogger.LogInfo($"Current Suggestion: {dtRunner.CurrentSuggestion}");
            MyLogger.LogInfo($"Last Suggestion: {dtRunner.LastSuggestion}");
            dtRunner.DebugDecisionTreeStatus();
        }
        
        MyLogger.LogInfo("Enhanced status logging completed");
    }
}
