#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class GraphAssetGizmos : MonoBehaviour
{
    public GraphAsset graph;
    public int highlightNode = -1;     // opcional: para remarcar alguno
    public int safeNodeIndex = -1;     // poné acá el mismo valor que usás en Civilian.fleeTargetNodeIndex

    [Header("Visual")]
    public float nodeRadius = 0.1f;
    public Color nodeColor = new Color(1f, 1f, 0f, 0.9f);
    public Color edgeColor = new Color(0f, 0.7f, 1f, 0.6f);
    public Color labelColor = new Color(1f, 1f, 1f, 1f);
    public Color safeColor = new Color(1f, 0.4f, 0.1f, 1f);
    public float safeRadius = 0.15f;

    private void OnDrawGizmos()
    {
        if (graph == null || graph.NodeCount == 0) return;

        // Edges
        Gizmos.color = edgeColor;
        for (int i = 0; i < graph.NodeCount; i++)
        {
            var pi = graph.nodePositions[i];
            var neigh = graph.neighbors[i].data;
            for (int k = 0; k < neigh.Length; k++)
            {
                int j = neigh[k];
                if (j < 0 || j >= graph.NodeCount) continue;
                var pj = graph.nodePositions[j];
                Gizmos.DrawLine(pi, pj);
            }
        }

        // Nodes
        Gizmos.color = nodeColor;
        for (int i = 0; i < graph.NodeCount; i++)
            Gizmos.DrawSphere(graph.nodePositions[i], nodeRadius);

        // Safe node
        if (safeNodeIndex >= 0 && safeNodeIndex < graph.NodeCount)
        {
            Gizmos.color = safeColor;
            var p = graph.nodePositions[safeNodeIndex];
            Gizmos.DrawSphere(p, safeRadius);
            Gizmos.DrawWireSphere(p, safeRadius * 1.8f);
            Handles.Label(p + Vector3.up * 0.2f, $"SAFE [{safeNodeIndex}]");
        }

        // Highlight
        if (highlightNode >= 0 && highlightNode < graph.NodeCount)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(graph.nodePositions[highlightNode], nodeRadius * 1.8f);
        }

        // Index labels
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = labelColor;
        for (int i = 0; i < graph.NodeCount; i++)
        {
            Handles.Label(graph.nodePositions[i] + Vector3.up * 0.15f, $"[{i}]", style);
        }
    }
}
#endif