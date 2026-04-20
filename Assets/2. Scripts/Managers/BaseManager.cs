using UnityEngine;

public abstract class BaseManager : MonoBehaviour, IManager
{
    protected bool isInitialized = false;
    
    public bool IsInitialized => isInitialized;
    
    public virtual void Initialize()
    {
        if (isInitialized)
        {
            MyLogger.LogWarning($"{GetType().Name} is already initialized!");
            return;
        }
        
        OnInitialize();
        isInitialized = true;
    }
    
    public virtual void Shutdown()
    {
        if (!isInitialized)
        {
            return;
        }
        
        OnShutdown();
        isInitialized = false;
    }
    
    protected abstract void OnInitialize();
    protected virtual void OnShutdown() { }
    
    protected virtual void OnDestroy()
    {
        Shutdown();
    }
}