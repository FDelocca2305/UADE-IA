#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GraphBaker
{
    [MenuItem("Pathfinding/Bake Graph From Scene")]
    public static void Bake()
    {
        //var nodes = Object.FindObjectsOfType<GraphNode>();
        var nodes = Object.FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        if (nodes == null || nodes.Length == 0)
        {
            MyLogger.LogWarning("GraphBaker: No GraphNode found in scene.");
            return;
        }

        var index = new Dictionary<GraphNode, int>(nodes.Length);
        for (int i = 0; i < nodes.Length; i++) index[nodes[i]] = i;

        var asset = ScriptableObject.CreateInstance<GraphAsset>();
        asset.nodePositions = new List<Vector3>(nodes.Length);
        asset.neighbors = new List<GraphAsset.IntArray>(nodes.Length);

        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            asset.nodePositions.Add(n.transform.position);

            var list = new List<int>(n.neighbors.Count);
            foreach (var nb in n.neighbors)
            {
                if (nb && index.TryGetValue(nb, out int nbIdx)) list.Add(nbIdx);
            }
            asset.neighbors.Add(new GraphAsset.IntArray { data = list.ToArray() });
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save GraphAsset", "GraphAsset", "asset", "Choose where to save the baked GraphAsset");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            MyLogger.LogInfo($"GraphBaker: Saved GraphAsset at {path} (nodes={asset.NodeCount}).");
        }
        else
        {
            Object.DestroyImmediate(asset);
            MyLogger.LogInfo("GraphBaker: Canceled.");
        }
    }
}
#endif
