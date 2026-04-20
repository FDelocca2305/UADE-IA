using System.Collections.Generic;
using Scripts.FSM.Base.StateMachine;
using Services;
using Services.MicroServices.BlackboardService;
using UnityEngine;

/// <summary>
/// Leader for Ally faction.
/// Coordinates allied units and issues defensive commands based on player status.
/// </summary>
public class AllyLeader : Ally
{
    [Header("Ally Leader Data")]
    [SerializeField] private AllyLeaderDataSO leaderData;
    [field: SerializeField] public List<StateData> LeaderStateDataList { get; private set; } = new();

    [Header("Ally Leadership")] [Tooltip("Allies managed by this leader")] [SerializeField]
    private List<Ally> managedAllies = new List<Ally>();

    [Header("Tactical Commands")]
    [SerializeField] private float coverFireFireRate = 0.3f;
    [SerializeField] private float holdPerimeterDuration = 8f;

    private IBlackboardService blackboard;
    private StateMachine leaderStateMachine;
    private float nextTacticsUpdateTime;
    private bool isProtectingPlayer;
    private float leaderStateTimer;
    public float PlayerProtectThreshold => leaderData.PlayerProtectThreshold;
    public bool IsProtectingPlayer => isProtectingPlayer;
    public float LeaderStateTimer
    {
        get => leaderStateTimer;
        set => leaderStateTimer = value;
    }
    [SerializeField] private bool showLeaderStateLabel = true;
    protected string LeaderStateName => leaderStateMachine?.GetCurrentState()?.State?.StateName ?? "None";
    public void SetLeaderData(AllyLeaderDataSO data)
    {
        leaderData = data;
    }
    public float CoverFireFireRate => coverFireFireRate;
    public float HoldPerimeterDuration => holdPerimeterDuration;

    protected override void Awake()
    {
        base.Awake();
        if (leaderData == null)
        {
            Debug.LogError("[AllyLeader] leaderData (SO) es obligatorio. Deshabilitando componente.");
            enabled = false;
            return;
        }

        blackboard = ServiceLocator.Get<IBlackboardService>();
        InitializeLeaderFSM();
    }

    private void Start()
    {
        if (blackboard == null)
        {
            blackboard = ServiceLocator.Get<IBlackboardService>();
            if (blackboard == null)
            {
                Debug.LogWarning("[AllyLeader] Blackboard service not found at Start");
            }
        }

        // Auto-discover nearby allies if list is empty
        if (managedAllies.Count == 0)
        {
            DiscoverNearbyAllies();
        }
    }

    public override void MyUpdate()
    {
        base.MyUpdate();
        if (!IsAlive) return;

        EnsureBlackboard();

        if (leaderStateMachine != null)
        {
            leaderStateMachine.RunStateMachine();
        }
        else
        {
            UpdateTactics();
        }
    }

    private void UpdateTactics()
    {
        if (Time.time < nextTacticsUpdateTime) return;
        nextTacticsUpdateTime = Time.time + leaderData.TacticsUpdateInterval;

        MonitorPlayerHealth();
        ShareIntel();
    }

    private void MonitorPlayerHealth()
    {
        if (!HasPlayerHealthData()) return;

        float l_healthPercent = GetPlayerHealthPercent();

        if (l_healthPercent < leaderData.PlayerProtectThreshold && !isProtectingPlayer)
        {
            IssueProtectPlayerCommand();
        }
        else if (l_healthPercent > leaderData.PlayerProtectThreshold + 0.2f && isProtectingPlayer)
        {
            // Player recovered, resume normal behavior
            ClearAllOverrides();
            isProtectingPlayer = false;
            Debug.Log("[AllyLeader] Player health recovered, resuming normal tactics");
        }
    }

    public void IssueProtectPlayerCommand()
    {
        Transform l_player = GetPlayerToFollow();
        if (l_player == null)
        {
            Debug.LogWarning("[AllyLeader] Cannot issue protect command - player reference is null");
            return;
        }

        Vector3 l_playerPos = l_player.position;
        int l_validAllies = 0;

        if (managedAllies == null || managedAllies.Count == 0)
        {
            Debug.LogWarning("[AllyLeader] No allies to command in protect order");
            return;
        }

        for (int i = 0; i < managedAllies.Count; i++)
        {
            if (managedAllies[i] == null || !managedAllies[i].IsAlive) continue;

            // Position allies in circle around player
            float l_angle = (360f / managedAllies.Count) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * leaderData.DefensiveRadius,
                0f,
                Mathf.Sin(l_angle) * leaderData.DefensiveRadius
            );

            Vector3 l_defensivePos = l_playerPos + l_offset;

            // Use Guard's Leader override system
            managedAllies[i].SetLeaderOverride(l_defensivePos, leaderData.DefensiveCommandDuration, "PROTECT", this, 2);
            l_validAllies++;
        }

        if (l_validAllies > 0)
        {
            isProtectingPlayer = true;

            // Share status via blackboard
            if (blackboard != null)
            {
                blackboard.SetValue("ALLIES_PROTECTING_PLAYER", true);
                blackboard.SetValue("ALLY_LEADER_POSITION", transform.position);
            }

            Debug.Log($"[AllyLeader] Issued protect command to {l_validAllies} allies");
        }
    }

    public void IssueEscortPlayerCommand()
    {
        Transform l_player = GetPlayerToFollow();
        if (l_player == null)
        {
            Debug.LogWarning("[AllyLeader] Cannot issue escort command - player reference is null");
            return;
        }

        if (managedAllies == null || managedAllies.Count == 0)
        {
            Debug.LogWarning("[AllyLeader] No allies to command in escort order");
            return;
        }

        Vector3 l_playerPos = l_player.position;
        int l_validAllies = 0;

        for (int i = 0; i < managedAllies.Count; i++)
        {
            if (managedAllies[i] == null || !managedAllies[i].IsAlive) continue;

            float l_angle = (360f / managedAllies.Count) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * leaderData.DefensiveRadius,
                0f,
                Mathf.Sin(l_angle) * leaderData.DefensiveRadius
            );

            Vector3 l_escortPos = l_playerPos + l_offset;
            managedAllies[i].SetLeaderOverride(l_escortPos, leaderData.DefensiveCommandDuration, "ESCORT", this, 1);
            l_validAllies++;
        }

        if (l_validAllies > 0)
        {
            isProtectingPlayer = false;

            if (blackboard != null)
            {
                blackboard.SetValue("ALLIES_PROTECTING_PLAYER", false);
                blackboard.SetValue("ALLY_LEADER_POSITION", transform.position);
            }

            Debug.Log($"[AllyLeader] Issued escort command to {l_validAllies} allies");
        }
    }

    public void IssueRegroupOnLeader()
    {
        if (managedAllies == null || managedAllies.Count == 0)
        {
            Debug.LogWarning("[AllyLeader] No allies available to regroup");
            return;
        }

        Vector3 l_center = transform.position;
        int l_validAllies = 0;

        for (int i = 0; i < managedAllies.Count; i++)
        {
            if (managedAllies[i] == null || !managedAllies[i].IsAlive) continue;

            float l_angle = (360f / managedAllies.Count) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * leaderData.DefensiveRadius,
                0f,
                Mathf.Sin(l_angle) * leaderData.DefensiveRadius
            );

            Vector3 l_regroupPos = l_center + l_offset;
            managedAllies[i].SetLeaderOverride(l_regroupPos, leaderData.DefensiveCommandDuration, "REGROUP", this, 1);
            l_validAllies++;
        }

        if (l_validAllies > 0 && blackboard != null)
        {
            blackboard.SetValue("ALLY_LEADER_POSITION", transform.position);
        }

        Debug.Log($"[AllyLeader] Regroup order issued to {l_validAllies} allies");
    }

    public void IssueHoldPerimeter(Vector3 center, string role = "HOLD")
    {
        if (managedAllies == null || managedAllies.Count == 0)
        {
            Debug.LogWarning("[AllyLeader] No allies to command in hold perimeter");
            return;
        }

        int l_validAllies = 0;
        for (int i = 0; i < managedAllies.Count; i++)
        {
            if (managedAllies[i] == null || !managedAllies[i].IsAlive) continue;

            float l_angle = (360f / managedAllies.Count) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * leaderData.DefensiveRadius,
                0f,
                Mathf.Sin(l_angle) * leaderData.DefensiveRadius
            );

            Vector3 l_pos = center + l_offset;
            managedAllies[i].SetLeaderOverride(l_pos, holdPerimeterDuration, role, this, 2);
            l_validAllies++;
        }

        if (l_validAllies > 0 && blackboard != null)
        {
            blackboard.SetValue("ALLY_LEADER_POSITION", center);
        }

        Debug.Log($"[AllyLeader] Issued hold perimeter to {l_validAllies} allies at {center}");
    }

    public void IssueAttackCommandOnTarget(Transform p_target)
    {
        if (p_target == null)
        {
            Debug.LogWarning("[AllyLeader] Cannot issue attack command - target is null");
            return;
        }

        if (managedAllies == null || managedAllies.Count == 0)
        {
            Debug.LogWarning("[AllyLeader] No allies to command in attack order");
            return;
        }

        Vector3 l_targetPos = p_target.position;
        int l_validAllies = 0;

        for (int i = 0; i < managedAllies.Count; i++)
        {
            if (managedAllies[i] == null || !managedAllies[i].IsAlive) continue;

            float l_angle = (360f / managedAllies.Count) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * (leaderData.DefensiveRadius * 0.5f),
                0f,
                Mathf.Sin(l_angle) * (leaderData.DefensiveRadius * 0.5f)
            );

            Vector3 l_attackPos = l_targetPos + l_offset;
            managedAllies[i].SetLeaderOverride(l_attackPos, leaderData.DefensiveCommandDuration, "ATTACK", this, 1);
            l_validAllies++;
        }

        if (l_validAllies > 0 && blackboard != null)
        {
            blackboard.SetValue("ALLY_TARGET_POSITION", l_targetPos);
        }

        Debug.Log($"[AllyLeader] Issued attack command to {l_validAllies} allies");
    }

    public void ShareIntel()
    {
        if (blackboard == null) return;

        // Share detected Guards with other allies via blackboard
        Guard l_currentTarget = GetCurrentTarget();
        if (l_currentTarget != null)
        {
            blackboard.SetValue("ALLY_DETECTED_GUARD_POSITION", l_currentTarget.transform.position);
            blackboard.SetValue("ALLY_DETECTED_GUARD_TIME", Time.time);
        }

        // Update ally count
        int l_aliveAllies = GetAliveAlliesCount();
        if (blackboard != null)
        {
            blackboard.SetValue("ALLIES_ALIVE_COUNT", l_aliveAllies);
        }
    }

    public void DiscoverNearbyAllies()
    {
        Collider[] l_colliders = Physics.OverlapSphere(transform.position, leaderData.AllyDiscoveryRadius);

        foreach (Collider l_col in l_colliders)
        {
            Ally l_ally = l_col.GetComponent<Ally>();
            if (l_ally != null && l_ally != this && !managedAllies.Contains(l_ally))
            {
                managedAllies.Add(l_ally);
            }
        }

        Debug.Log($"[AllyLeader] Discovered {managedAllies.Count} nearby allies");
    }

    public void ClearAllOverrides()
    {
        if (managedAllies == null) return;

        foreach (Ally l_ally in managedAllies)
        {
            if (l_ally == null) continue;
            l_ally.ClearLeaderOverride(this);
        }

        isProtectingPlayer = false;
        if (blackboard != null)
        {
            blackboard.SetValue("ALLIES_PROTECTING_PLAYER", false);
        }
    }

    public void AddManagedAlly(Ally p_ally)
    {
        if (p_ally != null && !managedAllies.Contains(p_ally))
        {
            managedAllies.Add(p_ally);
        }
    }

    public void RemoveManagedAlly(Ally p_ally)
    {
        if (p_ally != null)
        {
            managedAllies.Remove(p_ally);
        }
    }

    /// <summary>
    /// Set the list of allies managed by this leader. Used by spawners.
    /// </summary>
    public void SetManagedAllies(List<Ally> allies)
    {
        managedAllies = allies ?? new List<Ally>();
        Debug.Log($"[AllyLeader] {name} assigned {managedAllies.Count} Allies");
    }

    public bool HasPlayerHealthData()
    {
        return blackboard != null
               && blackboard.HasKey("PLAYER_HEALTH")
               && blackboard.HasKey("PLAYER_MAX_HEALTH");
    }

    public float GetPlayerHealthPercent()
    {
        if (!HasPlayerHealthData()) return 1f;

        float l_playerHealth = blackboard.GetValue<float>("PLAYER_HEALTH");
        float l_playerMaxHealth = Mathf.Max(blackboard.GetValue<float>("PLAYER_MAX_HEALTH"), 0.01f);
        return l_playerHealth / l_playerMaxHealth;
    }

    public bool HasAvailableAllies()
    {
        if (managedAllies == null) return false;
        foreach (var ally in managedAllies)
        {
            if (ally != null && ally.IsAlive)
                return true;
        }
        return false;
    }

    public int GetAliveAlliesCount()
    {
        if (managedAllies == null) return 0;

        int l_aliveAllies = 0;
        foreach (Ally l_ally in managedAllies)
        {
            if (l_ally != null && l_ally.IsAlive)
                l_aliveAllies++;
        }

        return l_aliveAllies;
    }

    private void InitializeLeaderFSM()
    {
        var leaderStates = LeaderStateDataList;
        if (leaderStates == null || leaderStates.Count == 0)
            return;

        leaderStateMachine = new StateMachine(leaderStates, this);
        MyLogger.LogInfo($"[AllyLeader] {name}: FSM inicializada con {leaderStates.Count} estados");
    }

    private void EnsureBlackboard()
    {
        if (blackboard != null) return;
        blackboard = ServiceLocator.Get<IBlackboardService>();
        if (blackboard == null)
        {
            Debug.LogWarning("[AllyLeader] Blackboard service still NULL");
        }
    }

    public virtual void ResetLeaderFromPool()
    {
        ClearAllOverrides();
        leaderStateTimer = 0f;
        nextTacticsUpdateTime = 0f;
        isProtectingPlayer = false;
        leaderStateMachine?.ResetStateMachine();
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showLeaderStateLabel) return;

        string allyState = GetCurrentStateName();
        string leaderState = LeaderStateName;

        FsmGizmoHelper.DrawStateLabel(transform, $"Ally:{allyState} | Leader:{leaderState}", Color.magenta, 3f);
    }
#endif

}
