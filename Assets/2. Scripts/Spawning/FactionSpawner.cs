using UnityEngine;
using System.Collections.Generic;
using Game.Spawning;
using Game.AI.Flocking;
using Services;
using Services.MicroServices.PoolObjectsService;

public class FactionSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Spawner configuration (ScriptableObject)")]
    [SerializeField] private FactionSpawnerConfig config;

    [Header("Spawn Settings")]
    [Tooltip("Center point for spawn pattern")]
    [SerializeField] private Transform spawnCenter;

    [Tooltip("Manual spawn points (for Manual pattern)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Shared waypoints for Guards (for SharedGroup mode)")]
    [SerializeField] private Transform[] sharedWaypoints;

    [Tooltip("Player reference (for Allies)")]
    [SerializeField] private Transform playerTarget;

    [Header("Runtime")]
    [Tooltip("Spawn faction on Start")]
    [SerializeField] private bool spawnOnStart = true;

    [Header("Debug")]
    [Tooltip("Show spawn preview gizmos in scene")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Color for gizmos")]
    [SerializeField] private Color gizmoColor = Color.green;

    // Runtime data
    private List<GameObject> spawnedUnits = new List<GameObject>();
    private GameObject spawnedLeader;
    private List<Transform> generatedWaypoints = new List<Transform>();
    private Transform waypointContainer;
    private IPoolObjectsService poolService;
    private readonly List<Transform> waypointPool = new List<Transform>();

    private void Start()
    {
        if (spawnOnStart)
        {
            if (config.spawnDelay > 0)
                Invoke(nameof(SpawnFaction), config.spawnDelay);
            else
                SpawnFaction();
        }
    }

    [ContextMenu("Spawn Faction")]
    public void SpawnFaction()
    {
        if (config == null)
        {
            Debug.LogError("[FactionSpawner] No config assigned!");
            return;
        }

        ClearPreviousSpawns();

        if (spawnCenter == null)
            spawnCenter = transform;

        if (poolService == null)
            poolService = ServiceLocator.Get<IPoolObjectsService>();

        if (poolService == null)
        {
            Debug.LogError("[FactionSpawner] PoolObjectsService is required to spawn factions.");
            return;
        }

        PreloadPool();
        StartCoroutine(SpawnSequence());
    }

    private System.Collections.IEnumerator SpawnSequence()
    {
        int waves = Mathf.Max(1, config.wavesCount);
        bool l_shouldSpawnLeader = ShouldSpawnLeader();

        // Generate waypoints once if needed (for Guards)
        if (config.factionType == FactionType.Guards)
        {
            GenerateWaypoints();
        }

        for (int wave = 0; wave < waves; wave++)
        {
            bool spawnLeaderThisWave = l_shouldSpawnLeader && wave == 0;

            // Spawn Leader (only first wave)
            if (spawnLeaderThisWave && config.leaderPrefab != null)
            {
                Vector3 l_leaderPos = GetSpawnPosition(0);
                spawnedLeader = SpawnUnit(config.leaderPrefab, l_leaderPos, true);
                if (config.delayBetweenSpawns > 0)
                    yield return new WaitForSeconds(config.delayBetweenSpawns);
            }

            // Spawn units for this wave
            for (int i = 0; i < config.unitsToSpawn; i++)
            {
                Vector3 l_spawnPos = GetSpawnPosition(i + (spawnLeaderThisWave ? 1 : 0));
                GameObject l_unit = SpawnUnit(config.unitPrefab, l_spawnPos, false);
                spawnedUnits.Add(l_unit);

                if (config.delayBetweenSpawns > 0)
                    yield return new WaitForSeconds(config.delayBetweenSpawns);
            }

            // Configure Leader once
            if (spawnLeaderThisWave)
                ConfigureLeader();
            
            if (wave < waves - 1 && config.delayBetweenWaves > 0)
                yield return new WaitForSeconds(config.delayBetweenWaves);
        }
    }

    private GameObject SpawnUnit(GameObject p_prefab, Vector3 p_position, bool p_isLeader)
    {
        if (p_prefab == null)
        {
            Debug.LogError("[FactionSpawner] Prefab is null!");
            return null;
        }

        GameObject l_unit = GetFromPoolOrInstantiate(p_prefab);
        if (l_unit == null)
        {
            Debug.LogError("[FactionSpawner] Could not spawn unit");
            return null;
        }

        // Place under spawner
        l_unit.transform.SetParent(transform);
        l_unit.transform.position = p_position;
        l_unit.transform.rotation = Quaternion.identity;

        // Reset basic state before configure/activation
        ResetPooledUnit(l_unit);

        l_unit.name = $"{config.factionType}_{(p_isLeader ? "Leader" : spawnedUnits.Count.ToString("00"))}";

        // Configure before activation
        if (config.factionType == FactionType.Guards)
        {
            ConfigureGuard(l_unit, p_isLeader);
        }
        else if (config.factionType == FactionType.Allies)
        {
            ConfigureAlly(l_unit, p_isLeader);
        }
        else if (config.factionType == FactionType.Civilians)
        {
            ConfigureCivilian(l_unit);
        }

        // Activate after configuration
        l_unit.SetActive(true);

        return l_unit;
    }

    private GameObject GetFromPoolOrInstantiate(GameObject prefab)
    {
        GameObject instance = null;

        if (poolService != null)
        {
            instance = poolService.GetOrCreateObject(prefab);
        }

        if (instance == null)
        {
            Debug.LogError("[FactionSpawner] Pool service returned null instance.");
            return null;
        }

        // Ensure inactive for configuration
        if (instance.activeSelf)
            instance.SetActive(false);

        return instance;
    }

    private void ResetPooledUnit(GameObject unit)
    {
        if (unit == null) return;

        // Reset base character health/state if present
        BaseCharacter character = unit.GetComponent<BaseCharacter>();
        if (character != null)
        {
            character.Initialize();
        }

        // Type-specific resets
        Guard guard = unit.GetComponent<Guard>();
        if (guard != null)
        {
            guard.ResetFromPool();
        }

        Ally ally = unit.GetComponent<Ally>();
        if (ally != null && guard == null) // avoid double reset if Guard inherits Ally someday
        {
            ally.ResetFromPool();
        }

        Leader leader = unit.GetComponent<Leader>();
        if (leader != null)
        {
            leader.ResetLeaderFromPool();
        }

        AllyLeader allyLeader = unit.GetComponent<AllyLeader>();
        if (allyLeader != null)
        {
            allyLeader.ResetLeaderFromPool();
        }
    }

    #region Guard Configuration

    private void ConfigureGuard(GameObject p_guardObj, bool p_isLeader)
    {
        Guard l_guard = p_guardObj.GetComponent<Guard>();
        if (l_guard == null)
        {
            Debug.LogError($"[FactionSpawner] {p_guardObj.name} doesn't have Guard component!");
            return;
        }

        // Assign waypoints
        Transform[] l_waypoints = GetWaypointsForGuard(spawnedUnits.Count);
        AssignWaypointsToGuard(l_guard, l_waypoints);
    }

    private void GenerateWaypoints()
    {
        if (config.waypointMode == WaypointGenerationMode.None)
            return;

        if (config.waypointMode == WaypointGenerationMode.SharedGroup)
        {
            // Use pre-configured waypoints
            if (sharedWaypoints != null && sharedWaypoints.Length > 0)
                return;

            Debug.LogWarning("[FactionSpawner] SharedGroup mode but no waypoints assigned! Using AutoCircle fallback.");
        }

        // Clear previous waypoints
        ClearGeneratedWaypoints();

        // Create container
        CreateWaypointContainer();

        switch (config.waypointMode)
        {
            case WaypointGenerationMode.AutoCircle:
                GenerateCircleWaypoints();
                break;
            case WaypointGenerationMode.AutoPatrol:
                GeneratePatrolWaypoints();
                break;
            case WaypointGenerationMode.Individual:
                GenerateIndividualWaypoints();
                break;
        }
    }

    private void CreateWaypointContainer()
    {
        if (waypointContainer == null)
        {
            GameObject l_container = new GameObject($"Waypoints_{config.factionType}");
            l_container.transform.parent = transform;
            l_container.transform.position = spawnCenter.position;
            waypointContainer = l_container.transform;
        }
    }

    private void GenerateCircleWaypoints()
    {
        // One set of waypoints in circle for all Guards
        for (int i = 0; i < config.waypointsPerGuard; i++)
        {
            float l_angle = (360f / config.waypointsPerGuard) * i * Mathf.Deg2Rad;
            Vector3 l_offset = new Vector3(
                Mathf.Cos(l_angle) * config.waypointRadius,
                0f,
                Mathf.Sin(l_angle) * config.waypointRadius
            );

            Transform l_wp = GetWaypointFromPool($"Waypoint_{i}", spawnCenter.position + l_offset);
            generatedWaypoints.Add(l_wp);
        }
    }

    private void GeneratePatrolWaypoints()
    {
        // Line of waypoints
        Vector3 l_start = spawnCenter.position - spawnCenter.forward * (config.waypointRadius * 0.5f);
        Vector3 l_end = spawnCenter.position + spawnCenter.forward * (config.waypointRadius * 0.5f);

        for (int i = 0; i < config.waypointsPerGuard; i++)
        {
            float l_t = i / (float)(config.waypointsPerGuard - 1);
            Vector3 l_pos = Vector3.Lerp(l_start, l_end, l_t);

            Transform l_wp = GetWaypointFromPool($"Waypoint_{i}", l_pos);
            generatedWaypoints.Add(l_wp);
        }
    }

    private void GenerateIndividualWaypoints()
    {
        // Generate separate waypoints for each Guard
        for (int l_guardIndex = 0; l_guardIndex < config.unitsToSpawn; l_guardIndex++)
        {
            Vector3 l_guardSpawnPos = GetSpawnPosition(l_guardIndex + (ShouldSpawnLeader() ? 1 : 0));

            for (int l_wpIndex = 0; l_wpIndex < config.waypointsPerGuard; l_wpIndex++)
            {
                float l_angle = (360f / config.waypointsPerGuard) * l_wpIndex * Mathf.Deg2Rad;
                Vector3 l_offset = new Vector3(
                    Mathf.Cos(l_angle) * (config.waypointRadius * 0.5f),
                    0f,
                    Mathf.Sin(l_angle) * (config.waypointRadius * 0.5f)
                );

                Transform l_wp = GetWaypointFromPool($"Guard{l_guardIndex}_Waypoint_{l_wpIndex}", l_guardSpawnPos + l_offset);
                generatedWaypoints.Add(l_wp);
            }
        }
    }

    private Transform[] GetWaypointsForGuard(int p_guardIndex)
    {
        switch (config.waypointMode)
        {
            case WaypointGenerationMode.SharedGroup:
                return sharedWaypoints;

            case WaypointGenerationMode.AutoCircle:
            case WaypointGenerationMode.AutoPatrol:
                // All Guards share generated waypoints
                return generatedWaypoints.ToArray();

            case WaypointGenerationMode.Individual:
                // Extract individual waypoints for this Guard
                int l_startIndex = p_guardIndex * config.waypointsPerGuard;
                Transform[] l_individualWaypoints = new Transform[config.waypointsPerGuard];
                for (int i = 0; i < config.waypointsPerGuard; i++)
                {
                    if (l_startIndex + i < generatedWaypoints.Count)
                        l_individualWaypoints[i] = generatedWaypoints[l_startIndex + i];
                }
                return l_individualWaypoints;

            default:
                return new Transform[0];
        }
    }

    private void AssignWaypointsToGuard(Guard p_guard, Transform[] p_waypoints)
    {
        if (p_waypoints == null || p_waypoints.Length == 0)
        {
            Debug.LogWarning($"[FactionSpawner] No waypoints to assign to {p_guard.name}");
            return;
        }

        // Use public method instead of reflection
        p_guard.SetPatrolPoints(p_waypoints);
    }

    #endregion

    #region Ally Configuration

    private void ConfigureAlly(GameObject p_allyObj, bool p_isLeader)
    {
        Ally l_ally = p_allyObj.GetComponent<Ally>();
        if (l_ally == null)
        {
            Debug.LogError($"[FactionSpawner] {p_allyObj.name} doesn't have Ally component!");
            return;
        }

        // Assign player reference
        if (playerTarget == null)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTarget == null)
            {
                Debug.LogError("[FactionSpawner] No Player found! Assign playerTarget manually.");
                return;
            }
            else
            {
                Debug.Log($"[FactionSpawner] Found Player by tag: {playerTarget.name}");
            }
        }

        l_ally.SetPlayerToFollow(playerTarget);
        Debug.Log($"[FactionSpawner] Assigned Player '{playerTarget.name}' to Ally '{l_ally.name}'");

        // Configure flocking
        FlockingSystem.FlockingEntity l_flockingEntity = p_allyObj.GetComponent<FlockingSystem.FlockingEntity>();
        if (l_flockingEntity != null && config.allyFlockingProfile != null)
        {
            l_flockingEntity.SetProfile(config.allyFlockingProfile);
        }
    }

    #endregion

    #region Leader Configuration

    private void ConfigureLeader()
    {
        if (!config.spawnLeader || spawnedLeader == null)
            return;

        if (config.factionType == FactionType.Guards)
        {
            Leader l_leader = spawnedLeader.GetComponent<Leader>();
            if (l_leader != null)
            {
                AssignGuardsToLeader(l_leader);
            }
        }
        else if (config.factionType == FactionType.Allies)
        {
            AllyLeader l_allyLeader = spawnedLeader.GetComponent<AllyLeader>();
            if (l_allyLeader != null)
            {
                AssignAlliesToLeader(l_allyLeader);
            }
        }
    }

    private void AssignGuardsToLeader(Leader p_leader)
    {
        List<Guard> l_guards = new List<Guard>();
        foreach (GameObject l_unit in spawnedUnits)
        {
            Guard l_guard = l_unit.GetComponent<Guard>();
            if (l_guard != null)
                l_guards.Add(l_guard);
        }

        p_leader.SetManagedGuards(l_guards);
    }

    private void AssignAlliesToLeader(AllyLeader p_allyLeader)
    {
        List<Ally> l_allies = new List<Ally>();
        foreach (GameObject l_unit in spawnedUnits)
        {
            Ally l_ally = l_unit.GetComponent<Ally>();
            if (l_ally != null)
                l_allies.Add(l_ally);
        }

        p_allyLeader.SetManagedAllies(l_allies);
    }

    #endregion

    #region Civilian Configuration

    private void ConfigureCivilian(GameObject p_civilianObj)
    {
        Civilian l_civilian = p_civilianObj.GetComponent<Civilian>();
        if (l_civilian == null)
        {
            Debug.LogWarning($"[FactionSpawner] {p_civilianObj.name} no tiene componente Civilian");
            return;
        }

        // No requiere líder ni waypoints automáticos
        Debug.Log($"[FactionSpawner] Civilian configurado: {l_civilian.name}");
    }

    #endregion

    #region Spawn Position Calculation

    private Vector3 GetSpawnPosition(int p_index)
    {
        int l_totalWithLeader = config.unitsToSpawn + (ShouldSpawnLeader() ? 1 : 0);

        switch (config.spawnPattern)
        {
            case SpawnPattern.Manual:
                if (spawnPoints != null && spawnPoints.Length > 0)
                    return spawnPoints[p_index % spawnPoints.Length].position;
                break;

            case SpawnPattern.Circle:
                float l_angle = (360f / Mathf.Max(l_totalWithLeader, 1)) * p_index * Mathf.Deg2Rad;
                Vector3 l_offset = new Vector3(
                    Mathf.Cos(l_angle) * config.spawnRadius,
                    0f,
                    Mathf.Sin(l_angle) * config.spawnRadius
                );
                return spawnCenter.position + l_offset;

            case SpawnPattern.Grid:
                int l_cols = Mathf.CeilToInt(config.gridSize.x);
                int l_row = p_index / l_cols;
                int l_col = p_index % l_cols;
                return spawnCenter.position + new Vector3(
                    (l_col - l_cols * 0.5f) * config.spacing,
                    0f,
                    (l_row - config.gridSize.y * 0.5f) * config.spacing
                );

            case SpawnPattern.Line:
                float l_linePos = (p_index - l_totalWithLeader * 0.5f) * config.spacing;
                return spawnCenter.position + spawnCenter.right * l_linePos;

            case SpawnPattern.Random:
                Vector2 l_randomCircle = Random.insideUnitCircle * config.spawnRadius;
                return spawnCenter.position + new Vector3(l_randomCircle.x, 0f, l_randomCircle.y);
        }

        return spawnCenter.position;
    }

    #endregion

    #region Utilities

    private bool ShouldSpawnLeader()
    {
        return config.spawnLeader && config.factionType != FactionType.Civilians;
    }

    private void PreloadPool()
    {
        if (poolService == null || config == null)
            return;

        int l_waves = Mathf.Max(1, config.wavesCount);
        int l_totalUnits = config.unitsToSpawn * l_waves;

        // Leaders only need one slot because they spawn on the first wave
        if (ShouldSpawnLeader())
            l_totalUnits += 1;

        // Preload regular units
        for (int i = 0; i < l_totalUnits; i++)
        {
            GameObject l_instance = poolService.GetOrCreateObject(config.unitPrefab);
            if (l_instance != null)
            {
                l_instance.SetActive(false);
                poolService.ReturnObject(l_instance);
            }
        }

        // Preload leader
        if (ShouldSpawnLeader() && config.leaderPrefab != null)
        {
            GameObject l_leaderInstance = poolService.GetOrCreateObject(config.leaderPrefab);
            if (l_leaderInstance != null)
            {
                l_leaderInstance.SetActive(false);
                poolService.ReturnObject(l_leaderInstance);
            }
        }
    }

    private Transform GetWaypointFromPool(string p_name, Vector3 p_position)
    {
        CreateWaypointContainer();

        Transform l_wp = null;
        if (waypointPool.Count > 0)
        {
            int l_lastIndex = waypointPool.Count - 1;
            l_wp = waypointPool[l_lastIndex];
            waypointPool.RemoveAt(l_lastIndex);
        }
        else
        {
            GameObject l_go = new GameObject(p_name);
            l_wp = l_go.transform;
        }

        l_wp.name = p_name;
        l_wp.SetParent(waypointContainer);
        l_wp.position = p_position;
        l_wp.rotation = Quaternion.identity;
        l_wp.gameObject.SetActive(true);

        return l_wp;
    }

    private void PoolWaypoint(Transform p_waypoint)
    {
        if (p_waypoint == null)
            return;

        p_waypoint.gameObject.SetActive(false);
        p_waypoint.SetParent(waypointContainer);
        waypointPool.Add(p_waypoint);
    }

    private void ReturnToPoolOrDestroy(GameObject obj)
    {
        if (obj == null) return;

        if (poolService != null)
        {
            poolService.ReturnObject(obj);
            return;
        }

        #if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(obj);
        else
        #endif
            Destroy(obj);
    }

    [ContextMenu("Clear Spawned Units")]
    public void ClearPreviousSpawns()
    {
        foreach (GameObject l_unit in spawnedUnits)
        {
            if (l_unit != null)
            {
                ReturnToPoolOrDestroy(l_unit);
            }
        }
        spawnedUnits.Clear();

        if (spawnedLeader != null)
        {
            ReturnToPoolOrDestroy(spawnedLeader);
        }

        ClearGeneratedWaypoints();
    }

    private void ClearGeneratedWaypoints()
    {
        foreach (Transform l_wp in generatedWaypoints)
        {
            if (l_wp != null)
            {
                PoolWaypoint(l_wp);
            }
        }
        generatedWaypoints.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || spawnCenter == null || config == null)
            return;

        Gizmos.color = gizmoColor;

        // Draw spawn positions preview
        bool l_hasLeader = ShouldSpawnLeader();
        int l_previewCount = config.unitsToSpawn + (l_hasLeader ? 1 : 0);
        for (int i = 0; i < l_previewCount; i++)
        {
            Vector3 l_pos = GetSpawnPosition(i);
            Gizmos.DrawWireSphere(l_pos, 0.5f);

            if (i == 0 && l_hasLeader)
            {
                Gizmos.DrawWireCube(l_pos + Vector3.up, Vector3.one * 0.3f); // Leader marker
            }
        }

        // Draw waypoint radius for Guards
        if (config.factionType == FactionType.Guards && config.waypointMode != WaypointGenerationMode.None)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter.position, config.waypointRadius);
        }

        // Draw spawn radius
        Gizmos.color = gizmoColor * 0.5f;
        Gizmos.DrawWireSphere(spawnCenter.position, config.spawnRadius);
    }

    #endregion
}
