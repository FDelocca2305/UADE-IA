using UnityEngine;

/// <summary>
/// Centraliza el estado de si el tutorial ya fue visto.
/// </summary>
public static class TutorialSeenService
{
    private const string TutorialSeenKey = "TutorialSeen";
    
    public static bool HasSeen()
    {
        return PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
    }

    public static void SetSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(TutorialSeenKey);
        PlayerPrefs.Save();
    }
}
