using Services.MicroServices.BlackboardService;
using Unity.VisualScripting;
using UnityEngine;

public static class MyGame
{
    private const string BLACKBOARD_SERVICE_SETTINGS_FILE_NAME = "BlackboardServiceSettings";

    private static BlackboardServiceSettings m_blackboardServiceSettings;
    public static BlackboardServiceSettings BlackboardServiceSettings => GetGameResource(ref m_blackboardServiceSettings, BLACKBOARD_SERVICE_SETTINGS_FILE_NAME);
    
    private static T GetGameResource<T>(ref T p_localVariable, string p_filePath) where T : ScriptableObject
    {
        if (p_localVariable != null)
            return p_localVariable;
        if (p_localVariable == null)
            p_localVariable = (T)Resources.Load(p_filePath, typeof(T));
        if (p_localVariable == null)
            MyLogger.LogError($"Asset '{p_filePath}' not found.");
        if (p_localVariable is IInitializable l_initializable)
        {
            l_initializable.Initialize();
        }          
        
        return p_localVariable;
    }
}
