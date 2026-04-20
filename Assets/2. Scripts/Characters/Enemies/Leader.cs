using System.Collections.Generic;
using Scripts.FSM.Base.StateMachine;
using Services;
using Services.MicroServices.BlackboardService;
using UnityEngine;

public class Leader : Guard
{
    [Header("Leader FSM")]
    [SerializeField] private List<StateData> leaderStateDataList = new List<StateData>();

    [Header("Team")]
    [SerializeField] private List<Guard> managedGuards = new List<Guard>();

    [Header("Reinforcements")]
    [SerializeField] private float requestPollInterval = 0.5f;
    [SerializeField] private int maxResponders = 2;
    [SerializeField] private float overrideDurationLeader = 8f;

    [Header("Hold Perimeter")]
    [SerializeField] private int holdSlots = 4;
    [SerializeField] private float holdRadius = 6f;
    [SerializeField] private float holdDuration = 6f;

    [Header("Leader Targeting")]
    [SerializeField] private float targetRescanInterval = 1.5f;

    [Header("Cover Fire")]
    [SerializeField] private float coverFireDuration = 4f;
    [SerializeField] private float coverFireFireRate = 0.25f;
    [SerializeField] private float coverFireDistanceThreshold = 3f;
    [Header("Debug")]
    [SerializeField] private bool showLeaderStateLabel = true;
    protected string LeaderStateName => leaderStateMachine?.GetCurrentState()?.State?.StateName ?? "None";

    private StateMachine leaderStateMachine;
    private IBlackboardService blackboard;
    private float nextPollTime;
    private float lastRequestTimeHandled = -1f;

    private bool hasPendingRequest;
    private Vector3 pendingRequestPos;
    private Vector3 pendingPlayerPos;

    private Vector3 lastHoldCenter;
    private float holdEndTime;
    private float leaderStateTimer;
    private float nextTargetScanTime;

    public bool HasPendingRequest => hasPendingRequest;
    public Vector3 PendingRequestPos => pendingRequestPos;
    public Vector3 PendingPlayerPos => pendingPlayerPos;
    public bool HoldComplete => Time.time >= holdEndTime;
    public float LeaderStateTimer
    {
        get => leaderStateTimer;
        set => leaderStateTimer = value;
    }
    public IReadOnlyList<Guard> ManagedGuards => managedGuards;
    public float CoverFireDuration => coverFireDuration;
    public float CoverFireFireRate => coverFireFireRate;
    public float CoverFireDistanceThreshold => coverFireDistanceThreshold;

    public override void Initialize()
    {
        base.Initialize();
        blackboard = ServiceLocator.Get<IBlackboardService>();
        InitializeLeaderFSM();

        if (blackboard == null)
        {
            MyLogger.LogWarning("[Leader] Blackboard service is NULL in Awake");
        }
        else
        {
            MyLogger.LogInfo("[Leader] Blackboard service acquired in Awake");
        }
    }

    private void Start()
    {
        if (blackboard == null)
        {
            blackboard = ServiceLocator.Get<IBlackboardService>();
            if (blackboard == null)
            {
                MyLogger.LogWarning("[Leader] Blackboard service not found at Start");
            }
            else
            {
                MyLogger.LogInfo("[Leader] Blackboard service acquired in Start");
            }
        }
    }

    private void Update()
    {
        if (blackboard == null)
        {
            blackboard = ServiceLocator.Get<IBlackboardService>();
            if (blackboard == null)
            {
                MyLogger.LogWarning("[Leader] Blackboard service still NULL in Update");
            }
            else
            {
                MyLogger.LogInfo("[Leader] Blackboard service acquired in Update");
            }
        }

        EnsurePlayerTarget();

        PollReinforcementRequests();
        leaderStateMachine?.RunStateMachine();
    }

    private void InitializeLeaderFSM()
    {
        if (leaderStateDataList == null || leaderStateDataList.Count == 0)
            return;

        leaderStateMachine = new StateMachine(leaderStateDataList, this);
    }

    private void PollReinforcementRequests()
    {
        if (blackboard == null) return;
        if (hasPendingRequest) return;
        if (Time.time < nextPollTime) return;
        nextPollTime = Time.time + requestPollInterval;

        float reqTime = blackboard.GetValue<float>("Reinforce_RequestTime");
        if (reqTime <= 0f || reqTime <= lastRequestTimeHandled) return;

        pendingRequestPos = blackboard.GetValue<Vector3>("Reinforce_TargetPosition");
        pendingPlayerPos = blackboard.GetValue<Vector3>("Reinforce_LastKnownPlayerPos");
        hasPendingRequest = true;

        MyLogger.LogInfo($"[Leader] Pending request detected time {reqTime}, center {pendingRequestPos}, lastPlayer {pendingPlayerPos}");
    }

    public void ConsumeRequest()
    {
        if (!hasPendingRequest) return;
        lastRequestTimeHandled = blackboard != null
            ? blackboard.GetValue<float>("Reinforce_RequestTime")
            : Time.time;
        hasPendingRequest = false;
        MyLogger.LogInfo($"[Leader] Consumed request time {lastRequestTimeHandled}");
    }

    public void AssignReinforcements(Vector3 center, Vector3 lastPlayerPos)
    {
        if (managedGuards == null) return;

        var responders = 0;
        foreach (var guard in managedGuards)
        {
            if (guard == null || !guard.IsActive || responders >= maxResponders)
                continue;

            if (guard.LeaderOverrideActive)
                continue;

            Vector3 offset = Random.insideUnitCircle.normalized * 2f;
            Vector3 target = center + new Vector3(offset.x, 0f, offset.y);

            guard.SetLeaderOverride(target, overrideDurationLeader, "reinforce");
            responders++;

            MyLogger.LogInfo($"[Leader] Assign reinforce to {guard.name} -> target {target}, center {center}, lastPlayer {lastPlayerPos}");
        }

        if (responders == 0)
        {
            MyLogger.LogInfo($"[Leader] No available guards to reinforce center {center}");
        }
    }

    public void BeginHold(Vector3 center)
    {
        lastHoldCenter = center;
        holdEndTime = Time.time + holdDuration;

        if (managedGuards == null) return;

        int slots = Mathf.Max(holdSlots, 1);
        for (int i = 0; i < managedGuards.Count; i++)
        {
            var guard = managedGuards[i];
            if (guard == null || !guard.IsActive) continue;

            float angle = (360f / slots) * (i % slots) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * holdRadius;
            Vector3 target = center + offset;

            guard.SetLeaderOverride(target, holdDuration, "hold");

            MyLogger.LogInfo($"[Leader] Assign hold to {guard.name} -> target {target}, slot {i % slots}, center {center}");
        }
    }

    public void ClearAllOverrides()
    {
        if (managedGuards == null) return;
        foreach (var guard in managedGuards)
        {
            if (guard == null) continue;
            guard.ClearLeaderOverride();
        }
    }

    public virtual void ResetLeaderFromPool()
    {
        ClearAllOverrides();
        hasPendingRequest = false;
        pendingRequestPos = Vector3.zero;
        pendingPlayerPos = Vector3.zero;
        lastHoldCenter = Vector3.zero;
        holdEndTime = 0f;
        leaderStateTimer = 0f;
        nextTargetScanTime = 0f;
        leaderStateMachine?.ResetStateMachine();
    }
    
    public void SetManagedGuards(List<Guard> guards)
    {
        managedGuards = guards ?? new List<Guard>();
        Debug.Log($"[Leader] {name} assigned {managedGuards.Count} Guards");
    }

    private void EnsurePlayerTarget()
    {
        if (Player != null) return;
        if (Time.time < nextTargetScanTime) return;
        nextTargetScanTime = Time.time + targetRescanInterval;

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var tag in targetTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
            if (candidates == null) continue;

            for (int i = 0; i < candidates.Length; i++)
            {
                float d = Vector3.Distance(transform.position, candidates[i].transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = candidates[i];
                }
            }
        }

        if (closest != null)
        {
            SetTargetTransform(closest.transform);
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showLeaderStateLabel) return;

        string guardState = CurrentStateName;
        string leaderState = LeaderStateName;
        FsmGizmoHelper.DrawStateLabel(transform, $"Guard:{guardState} | Leader:{leaderState}", Color.yellow, 3f);
    }
#endif

}
