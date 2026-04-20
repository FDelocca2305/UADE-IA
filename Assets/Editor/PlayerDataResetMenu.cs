using System.IO;
using Services;
using Services.MicroServices.UserDataService;
using UnityEditor;
using UnityEngine;

public static class PlayerDataResetMenu
{
    private const string MenuPath = "Tools/Player Data/Reset Persistent Data";

    [MenuItem(MenuPath)]
    private static void ResetPlayerData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "UserDataService.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Player data deleted: {filePath}");
        }
        else
        {
            Debug.Log("Player data file not found. Nothing to delete.");
        }

        if (Application.isPlaying)
        {
            var userData = ServiceLocator.Get<IUserDataService>();
            userData?.Save();
        }

        AssetDatabase.Refresh();
    }
}
