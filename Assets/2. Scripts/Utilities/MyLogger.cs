using System;
using UnityEngine;

public static class MyLogger
{
    public static void LogInfo(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[{DateTime.Now:HH:mm:ss}] {message}");
#endif
    }

    public static void LogWarning(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[{DateTime.Now:HH:mm:ss}] {message}");
#endif
    }

    public static void LogError(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError($"[{DateTime.Now:HH:mm:ss}] {message}");
#endif
    }

    public static void LogDebug(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[DEBUG] [{DateTime.Now:HH:mm:ss}] {message}");
#endif
    }

    public static void LogException(Exception exception)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogException(exception);
#endif
    }
}