using DevelopmentUtilities;
using UnityEngine;

namespace Services.MicroServices.PoolObjectsService
{
    public class PoolWrapper<T> : IPoolWrapper<T> where T : Object
    {
        private PoolGeneric<T> m_pool;
        
        public PoolWrapper(T p_prefab, Transform p_transformParent = null)
        {
            m_pool = new PoolGeneric<T>(p_prefab, p_transformParent);
        }
        
        public T GetOrCreate() => m_pool.GetorCreate();
        public void ReturnToPool(T p_toAdd) => m_pool.ReturnToPool(p_toAdd);
        public void Clear() => m_pool.ClearData();
    }
}