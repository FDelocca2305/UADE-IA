using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-50)]
public class NavMeshAgentPlacer : MonoBehaviour
{
    public NavMeshAgent agent;
    public float maxSnapDistance = 3f;

    void Reset() => agent = GetComponent<NavMeshAgent>();

    void OnEnable()
    {
        StartCoroutine(SnapNextFrame());
    }

    System.Collections.IEnumerator SnapNextFrame()
    {
        yield return null;
        TrySnapToNavMesh(agent, maxSnapDistance);
    }

    public static bool TrySnapToNavMesh(NavMeshAgent a, float maxDist)
    {
        if (!a) return false;
        if (a.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(a.transform.position, out var hit, maxDist, a.areaMask))
            return a.Warp(hit.position);

        MyLogger.LogError($"[{a.name}] No pude colocarlo en el NavMesh (buscado {maxDist}m).");
        return false;
    }
}
