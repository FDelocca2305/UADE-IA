using System.Collections.Generic;
using UnityEngine;

namespace DevelopmentUtilities
{
    public class PoolGeneric<T> where T : Object
    {
        private readonly T m_prefab;
        private readonly Transform m_parent;
        private readonly Queue<T> m_availables = new();

        public PoolGeneric(T p_prefab, Transform p_transformParent = null)
        {
            m_prefab = p_prefab;
            m_parent = p_transformParent;
        }

        public T GetorCreate()
        {
            if (m_availables.Count > 0)
            {
                var l_obj = m_availables.Dequeue();
                while (l_obj == null && m_availables.Count > 0)
                {
                    l_obj = m_availables.Dequeue();
                }

                if (l_obj == null)
                {
                    l_obj = InstantiateInactive();
                }

                return l_obj;
            }

            return InstantiateInactive();
        }

        public void ReturnToPool(T p_poolEntry)
        {
            if (p_poolEntry == null)
                return;

            SetActiveState(p_poolEntry, false);
            m_availables.Enqueue(p_poolEntry);
        }

        public void ClearData()
        {
            m_availables.Clear();
        }

        private T InstantiateInactive()
        {
            var l_instance = Object.Instantiate(m_prefab, m_parent);
            SetActiveState(l_instance, false);
            return l_instance;
        }

        private static void SetActiveState(T p_object, bool p_active)
        {
            if (p_object is Component l_component)
            {
                l_component.gameObject.SetActive(p_active);
            }
            else if (p_object is GameObject l_gameObject)
            {
                l_gameObject.SetActive(p_active);
            }
        }
    }
}
