#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GraphLoader : EditorWindow
{
    [MenuItem("Pathfinding/Load Graph Asset to Scene (Editable)")]
    public static void LoadGraphToScene()
    {
        // 1. Intentar encontrar el asset correcto
        GraphAsset assetToLoad = null;

        // A) Si tienes seleccionado el asset en el Project window, usa ese
        if (Selection.activeObject is GraphAsset)
        {
            assetToLoad = (GraphAsset)Selection.activeObject;
        }
        // B) Si no, busca el del SceneGraphManager de la escena
        else
        {
            var manager = Object.FindAnyObjectByType<SceneGraphManager>();
            if (manager != null)
            {
                assetToLoad = manager.currentLevelGraph;
            }
        }

        if (assetToLoad == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontro ningun GraphAsset.\n\nSelecciona uno en la carpeta del proyecto o asegurate de que el SceneGraphManager tenga uno asignado.", "Ok");
            return;
        }

        // 2. Preguntar seguridad antes de llenar la escena
        bool confirm = EditorUtility.DisplayDialog("Cargar Grafo",
            $"Se van a generar {assetToLoad.NodeCount} nodos en la escena basados en '{assetToLoad.name}'.\n\nEsto es para editar. ¿Continuar?",
            "Generar Nodos", "Cancelar");

        if (!confirm) return;

        // 3. Crear contenedor para no ensuciar la jerarquia
        GameObject container = new GameObject($"Graph_From_{assetToLoad.name}");
        Undo.RegisterCreatedObjectUndo(container, "Load Graph Nodes"); // Permitir Ctrl+Z

        // el gameObject se crea con el tag EditorOnly
        try
        {
            container.tag = "EditorOnly";
        }
        catch (UnityException)
        {
            Debug.LogWarning("El tag 'EditorOnly' no existe o fue borrado. Los nodos se crearon sin tag.");
        }
        // ---------------------------

        Undo.RegisterCreatedObjectUndo(container, "Load Graph Nodes");

        // Lista temporal para reconectar vecinos
        List<GraphNode> createdNodes = new List<GraphNode>();

        // 4. Instanciar los Nodos (Posición)
        for (int i = 0; i < assetToLoad.nodePositions.Count; i++)
        {
            GameObject nodeGO = new GameObject($"Node_{i}");
            nodeGO.transform.position = assetToLoad.nodePositions[i];
            nodeGO.transform.parent = container.transform;

            // Agregamos el componente GraphNode
            GraphNode script = nodeGO.AddComponent<GraphNode>();

            // Opcional: Cambiar el icono o color para que se vea
            // (El Gizmo del script GraphNode ya se encarga de dibujar la esfera)

            createdNodes.Add(script);
        }

        // 5. Reconectar Vecinos (Referencias)
        for (int i = 0; i < assetToLoad.neighbors.Count; i++)
        {
            GraphNode currentNode = createdNodes[i];
            int[] neighborIndices = assetToLoad.neighbors[i].data;

            foreach (int neighborIndex in neighborIndices)
            {
                // Validación de indice por seguridad
                if (neighborIndex >= 0 && neighborIndex < createdNodes.Count)
                {
                    // Evitar duplicados (aunque el bakeo limpia, mejor prevenir)
                    if (!currentNode.neighbors.Contains(createdNodes[neighborIndex]))
                    {
                        currentNode.neighbors.Add(createdNodes[neighborIndex]);
                    }
                }
            }
        }

        // 6. Finalizar
        Selection.activeGameObject = container;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"<color=green>SUCCESS:</color> Se generaron {createdNodes.Count} nodos editables desde '{assetToLoad.name}'.");
    }
}
#endif