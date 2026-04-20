using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool for any Component type - THE definitive pool implementation
/// </summary>
public class ObjectPool<T> where T : Component
{
    private int initialSize;
    private int maxSize;
    private Queue<T> availableObjects;
    private HashSet<T> activeObjects;
    private T prefab;
    private Transform parent;
    
    public int ActiveCount => activeObjects.Count;
    public int AvailableCount => availableObjects.Count;
    public int TotalCount => ActiveCount + AvailableCount;
    
    /// <summary>
    /// Create an object pool
    /// </summary>
    public ObjectPool(T prefab, Transform parent = null, int initialPoolSize = 10, int maxPoolSize = 100)
    {
        this.prefab = prefab;
        this.parent = parent;
        initialSize = initialPoolSize;
        maxSize = maxPoolSize;
        
        // Initialize collections
        availableObjects = new Queue<T>();
        activeObjects = new HashSet<T>();
        
        InitializePool();
    }
    
    
    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }
    
    private T CreateNewObject()
    {
        T newObject = UnityEngine.Object.Instantiate(prefab, parent);
        newObject.name = $"{prefab.name}_Pooled_{TotalCount}";
        newObject.gameObject.SetActive(false);
        
        availableObjects.Enqueue(newObject);
        return newObject;
    }
    
    /// <summary>
    /// Get object from pool
    /// </summary>
    public T Get()
    {
        T obj;
        
        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else
        {
            if (TotalCount >= maxSize)
            {
                MyLogger.LogWarning($"ObjectPool<{typeof(T).Name}>: Creating object beyond max size!");
            }
            obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = $"{prefab.name}_Pooled_{TotalCount}";
            obj.gameObject.SetActive(false);
        }
        
        activeObjects.Add(obj);
        obj.gameObject.SetActive(true);
        
        if (obj is IPoolable poolable)
        {
            poolable.OnPoolGet();
        }
        return obj;
    }
    
    /// <summary>
    /// Return object to pool
    /// </summary>
    public void Return(T obj)
    {
        if (!obj) return;
        
        if (!activeObjects.Contains(obj))
        {
            MyLogger.LogWarning($"ObjectPool<{typeof(T).Name}>: Trying to return object that wasn't from this pool!");
            return;
        }
        
        activeObjects.Remove(obj);
        
        if (obj is IPoolable returnPoolable)
        {
            returnPoolable.OnPoolReturn();
        }
        
        obj.gameObject.SetActive(false);
        availableObjects.Enqueue(obj);
    }
    
    /// <summary>
    /// Return all active objects to pool
    /// </summary>
    public void ReturnAll()
    {
        var objectsToReturn = new List<T>(activeObjects);
        foreach (var obj in objectsToReturn)
        {
            Return(obj);
        }
    }
    
    /// <summary>
    /// Clear the entire pool
    /// </summary>
    public void Clear()
    {
        ReturnAll();
        
        while (availableObjects.Count > 0)
        {
            T obj = availableObjects.Dequeue();
            if (obj)
            {
                if (obj is IPoolable destroyPoolable)
                {
                    destroyPoolable.OnPoolDestroy();
                }
                
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        
        activeObjects.Clear();
    }
}

public interface IPoolable
{
    void OnPoolGet();
    void OnPoolReturn();
    void OnPoolDestroy();
}