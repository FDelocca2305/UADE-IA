using UnityEngine;

/// <summary>
/// Mantiene la referencia al Grafo correcto para cada nivel.
/// </summary>
public class SceneGraphManager : MonoBehaviour
{
    public GraphAsset currentLevelGraph;

    public static SceneGraphManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}