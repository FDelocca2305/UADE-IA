using UnityEngine;
using ScriptableObjects.Bullets;

/// <summary>
/// Configuration data for Ally NPCs.
/// Allies escort the player and attack Guards using a priority-based behavior system.
/// Priority: Leader Override > Attack Guard > Follow Player
/// Note: Does NOT use FSM - uses simple priority system for efficiency.
/// </summary>
[CreateAssetMenu(fileName = "AllyData", menuName = "Game Data/Ally Data")]
public class AllyDataSO : NPCDataSO
{
    [Header("Ally Combat")]
    [Tooltip("Range at which Ally will attack detected Guards")]
    [field: SerializeField] public float attackRange { get; private set; } = 8f;

    [Tooltip("Speed when chasing Guards")]
    [field: SerializeField] public float chaseSpeed { get; private set; } = 5f;

    [field: SerializeField] public BulletData bulletData { get; private set; }

    [Header("Player Following")]
    [Tooltip("Desired distance from player when following")]
    [field: SerializeField] public float followDistance { get; private set; } = 3f;

    [Tooltip("Speed when following player")]
    [field: SerializeField] public float followSpeed { get; private set; } = 4f;
    [Tooltip("Margen extra para no pegarse al player (histeresis follow).")]
    [field: SerializeField] public float followDistanceBuffer { get; private set; } = 2f;

    [Header("Flocking Configuration")]
    [Tooltip("Enable flocking behavior for group movement")]
    [field: SerializeField] public bool useFlocking { get; private set; } = true;

    [Tooltip("Weight of follow player force (0-1)")]
    [Range(0f, 1f)]
    [field: SerializeField] public float followPlayerWeight { get; private set; } = 0.6f;

    [Tooltip("Weight of flocking force (0-1)")]
    [Range(0f, 1f)]
    [field: SerializeField] public float flockingWeight { get; private set; } = 0.4f;

    [Header("Steering Physics")]
    [field: SerializeField] public float mass { get; set; } = 1f;
    [field: SerializeField] public float maxForce { get; set; } = 20f;
    [field: SerializeField] public float maxSpeed { get; set; } = 6f;
    [field: SerializeField] public float slowingDistance { get; set; } = 2f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Layer mask for obstacles (walls, static objects). NEVER use 'Everything' or Ally will avoid player/guards!")]
    [field: SerializeField] public LayerMask obstaclesMask { get; set; } = 1 << 8;  // Layer 8 = Obstacles
    [field: SerializeField] public float avoidRadius { get; set; } = 2f;
    [field: SerializeField] public float avoidAngle { get; set; } = 90f;
    [field: SerializeField] public float personalArea { get; set; } = 0.5f;

    [Header("Investigation / Search")]
    [field: SerializeField] public float investigationRotateSpeed { get; private set; } = 180f;
    [field: SerializeField] public float investigationMoveSpeedFactor { get; private set; } = 0.7f;
    [field: SerializeField] public float investigationArrivalTolerance { get; private set; } = 1.2f;
    [field: SerializeField] public float searchDuration { get; private set; } = 4f;

    [Header("Cover Behavior")]
    [field: SerializeField] public float coverProbeRadius { get; private set; } = 0.9f;
    [field: SerializeField] public float coverProbeDistance { get; private set; } = 4f;
    [field: SerializeField] public float coverOffsetFromObstacle { get; private set; } = 1.25f;
    [field: SerializeField] public float coverRepositionCooldown { get; private set; } = 1.2f;
    [field: SerializeField] public float coverArrivalTolerance { get; private set; } = 0.9f;
    [field: SerializeField] public float coverHealthRegenPerSecond { get; private set; } = 10f;
    [field: SerializeField, Range(0f, 1f)] public float coverExitHealthPercent { get; private set; } = 0.75f;
    [field: SerializeField] public float coverMinDuration { get; private set; } = 1.5f;

    [Header("Smoke / Utility")]
    [field: SerializeField] public GameObject smokePrefab { get; private set; }
    [field: SerializeField] public float smokeLifetime { get; private set; } = 5f;
    [field: SerializeField] public float smokeScale { get; private set; } = 3f;
    [field: SerializeField] public float smokeCooldown { get; private set; } = 6f;
    [field: SerializeField] public string smokeObstacleLayerName { get; private set; } = "ObstacleAI";
}
