using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Ensures Enhanced Touch support remains active for the entire session.
/// Drop this component into the initial scene; it persists across scene loads.
/// </summary>
public class TouchInputManager : MonoBehaviour
{
    [SerializeField]
    private bool persistAcrossScenes = true;

    private static TouchInputManager s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
        
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!TouchSimulation.instance.enabled)
            TouchSimulation.Enable();
#endif
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;

            if (EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();
            }
        }
    }
}
