#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de Editor para que QA pueda resetear el flag del tutorial.
/// No requiere GameObject: se usa desde el menú o la ventana.
/// </summary>
public class TutorialResetTool : EditorWindow
{
    [MenuItem("Tools/Tutorial/Reset Seen")]
    private static void ResetFromMenu()
    {
        TutorialSeenService.Reset();
        Debug.Log("[TutorialResetTool] Tutorial reset (menu)");
    }

    [MenuItem("Tools/Tutorial/Reset Tool")]
    private static void ShowWindow()
    {
        var window = GetWindow<TutorialResetTool>("Tutorial Reset");
        window.minSize = new Vector2(260, 80);
    }

    private void OnGUI()
    {
        GUILayout.Label("Tutorial", EditorStyles.boldLabel);
        GUILayout.Label("Reiniciar el flag de 'tutorial visto' (PlayerPrefs).");

        if (GUILayout.Button("Reset Tutorial Seen"))
        {
            TutorialSeenService.Reset();
            Debug.Log("[TutorialResetTool] Tutorial reset (window)");
        }
    }
}
#endif
