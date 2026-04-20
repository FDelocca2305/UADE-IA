#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    [Tooltip("Conexiones manuales a otros nodos")]
    public List<GraphNode> neighbors = new();

    [Header("Gizmos")]
    public Color gizmoColor = Color.yellow;
    public float gizmoSize = 0.2f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSize);

        Gizmos.color = Color.cyan;

        foreach (var n in neighbors)
            if (n) Gizmos.DrawLine(transform.position, n.transform.position);

        // dibujar el nombre del GO (puede incluir el indice si lo renombras)
        //UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, gameObject.name);
    }
}
#endif