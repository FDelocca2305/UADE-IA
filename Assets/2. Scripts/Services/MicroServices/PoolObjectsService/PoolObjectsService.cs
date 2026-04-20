using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Services.MicroServices.PoolObjectsService
{
    public class PoolObjectsService : IPoolObjectsService
    {
        private Dictionary<Object, IPoolWrapper> m_prefabPools;
        private Dictionary<Object, IPoolWrapper> m_instanceLookup;
        
        public void Initialize()
        {
            m_prefabPools = new Dictionary<Object, IPoolWrapper>();
            m_instanceLookup = new Dictionary<Object, IPoolWrapper>();
        }

        public T GetOrCreateObject<T>(T p_prefab, Transform p_content = null) where T : Object
        {
            if (p_prefab == null)
                throw new ArgumentNullException(nameof(p_prefab), "Prefab cannot be null.");

            if (!m_prefabPools.TryGetValue(p_prefab, out var l_poolWrapper))
            {
                l_poolWrapper = new PoolWrapper<T>(p_prefab, p_content);
                m_prefabPools.Add(p_prefab, l_poolWrapper);
            }

            if (l_poolWrapper is not PoolWrapper<T> l_wrapper)
                throw new InvalidOperationException($"Pool wrapper for prefab {p_prefab.name} is not compatible with type {typeof(T)}.");

            var l_instance = l_wrapper.GetOrCreate();
            m_instanceLookup[l_instance] = l_poolWrapper;
            return l_instance;
        }

        public void ReturnObject<T>(T p_object) where T : Object
        {
            if (p_object == null)
                return;

            if (!m_instanceLookup.TryGetValue(p_object, out var l_poolWrapper))
            {
                Object.Destroy(p_object);
                return;
            }

            m_instanceLookup.Remove(p_object);

            if (l_poolWrapper is PoolWrapper<T> l_wrapper)
            {
                l_wrapper.ReturnToPool(p_object);
            }
            else
            {
                Object.Destroy(p_object);
            }
        }
    }
}