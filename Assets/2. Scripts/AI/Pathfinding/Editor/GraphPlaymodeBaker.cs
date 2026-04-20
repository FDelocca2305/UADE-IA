#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para obtener el nombre
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public static class GraphPlaymodeBaker
{
    static GraphPlaymodeBaker()
    {
        EditorApplication.playModeStateChanged += OnState;
    }

    static void OnState(PlayModeStateChange state)
    {
        // Solo ejecutar justo antes de entrar a Play Mode
        if (state != PlayModeStateChange.ExitingEditMode) return;

        //var nodes = Object.FindObjectsOfType<GraphNode>();
        var nodes = Object.FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        if (nodes == null || nodes.Length == 0) return;

        // === Nombre dinamico basado en la escena ===
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) sceneName = "Untitled";

        string folder = "Assets/AI/Graphs";
        string path = $"{folder}/Graph_{sceneName}.asset";
        // Ejemplo: Assets/AI/Graphs/Graph_Nivel1.asset
        // =========================================================

        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var asset = AssetDatabase.LoadAssetAtPath<GraphAsset>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<GraphAsset>();
            AssetDatabase.CreateAsset(asset, path);
        }

        var index = new Dictionary<GraphNode, int>(nodes.Length);
        for (int i = 0; i < nodes.Length; i++) index[nodes[i]] = i;

        asset.nodePositions.Clear();
        asset.neighbors.Clear();

        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            asset.nodePositions.Add(n.transform.position);

            var list = new List<int>(n.neighbors.Count);
            foreach (var nb in n.neighbors)
            {
                if (nb && index.TryGetValue(nb, out int id)) list.Add(id);
            }
            asset.neighbors.Add(new GraphAsset.IntArray { data = list.ToArray() });
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        // Asignar automaticamente el grafo al SceneGraphManager
        AssignToSceneManager(asset);

        Debug.Log($"GraphPlaymodeBaker: Baked '{path}' ({asset.NodeCount} nodes).");
    }

    // Pequeno helper para conectar todo automaticamente
    static void AssignToSceneManager(GraphAsset asset)
    {
        var manager = Object.FindAnyObjectByType<SceneGraphManager>();
        if (manager != null)
        {
            manager.currentLevelGraph = asset;
            EditorUtility.SetDirty(manager);
        }
    }
}
#endif