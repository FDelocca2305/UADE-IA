using UnityEngine;
using Game.AI.Flocking;
using Scripts.FSM.Models;

namespace Game.Spawning
{
    public enum FactionType
    {
        Guards,
        Allies,
        Civilians
    }

    public enum SpawnPattern
    {
        Manual,
        Circle,
        Grid,
        Line,
        Random
    }

    public enum WaypointGenerationMode
    {
        None,
        AutoCircle,
        AutoPatrol,
        SharedGroup,
        Individual
    }

    [CreateAssetMenu(fileName = "FactionSpawnerConfig", menuName = "Game Data/Faction Spawner Config")]
    public class FactionSpawnerConfig : ScriptableObject
    {
        [Header("Faction Settings")]
        [Tooltip("Type of faction to spawn")]
        [field: SerializeField] public FactionType factionType { get; private set; } = FactionType.Guards;

        [Tooltip("Number of units to spawn")]
        [field: SerializeField] public int unitsToSpawn { get; private set; } = 5;

        [Tooltip("Spawn a leader for this faction")]
        [field: SerializeField] public bool spawnLeader { get; private set; } = true;

        [Header("Waves")]
        [Tooltip("Number of waves to spawn (1 = single wave)")]
        [field: SerializeField] public int wavesCount { get; private set; } = 1;

        [Tooltip("Delay between waves (seconds)")]
        [field: SerializeField] public float delayBetweenWaves { get; private set; } = 1f;

        [Header("Prefab References")]
        [Tooltip("Prefab for regular unit (Guard or Ally)")]
        [field: SerializeField] public GameObject unitPrefab { get; private set; }

        [Tooltip("Prefab for leader unit")]
        [field: SerializeField] public GameObject leaderPrefab { get; private set; }
        // Se oculta para que el spawner no solicite este SO; el prefab trae su propia data
        [field: SerializeField, HideInInspector] public AllyLeaderDataSO allyLeaderData { get; private set; }

        [Header("Spawn Pattern")]
        [Tooltip("Pattern for unit placement")]
        [field: SerializeField] public SpawnPattern spawnPattern { get; private set; } = SpawnPattern.Circle;

        [Tooltip("Radius for Circle/Random patterns")]
        [field: SerializeField] public float spawnRadius { get; private set; } = 5f;

        [Tooltip("Grid dimensions (columns x rows)")]
        [field: SerializeField] public Vector2 gridSize { get; private set; } = new Vector2(3, 3);

        [Tooltip("Spacing between units")]
        [field: SerializeField] public float spacing { get; private set; } = 2f;

        [Header("Guard Waypoint Settings")]
        [Tooltip("How to generate/assign waypoints for Guards")]
        [field: SerializeField] public WaypointGenerationMode waypointMode { get; private set; } = WaypointGenerationMode.AutoCircle;

        [Tooltip("Radius for generated waypoint patrol")]
        [field: SerializeField] public float waypointRadius { get; private set; } = 8f;

        [Tooltip("Number of waypoints per Guard")]
        [field: SerializeField] public int waypointsPerGuard { get; private set; } = 4;

        [Header("Ally Settings")]
        [Tooltip("FlockingProfile to assign to Allies")]
        [field: SerializeField] public FlockingProfile allyFlockingProfile { get; private set; }

        [Tooltip("Distance Allies maintain from player")]
        [field: SerializeField] public float followDistance { get; private set; } = 3f;

        [Header("Timing")]
        [Tooltip("Delay before initial spawn")]
        [field: SerializeField] public float spawnDelay { get; private set; } = 0f;

        [Tooltip("Delay between spawning each unit")]
        [field: SerializeField] public float delayBetweenSpawns { get; private set; } = 0.1f;
    }
}
