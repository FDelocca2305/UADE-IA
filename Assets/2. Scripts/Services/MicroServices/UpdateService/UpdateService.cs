using System.Collections.Generic;

namespace Services.MicroServices.UpdateService
{
    public class UpdateService : IUpdateService
    {
        private List<IUpdateListener> m_updateListeners;
        private List<IFixedUpdateListener> m_fixedUpdateListeners;
        private List<ILateUpdateListener> m_lateUpdateListeners;
        public void Initialize()
        {
            m_updateListeners = new List<IUpdateListener>();
            m_fixedUpdateListeners = new List<IFixedUpdateListener>();
            m_lateUpdateListeners = new List<ILateUpdateListener>();
        }

        public void AddUpdateListener(IUpdateListener p_listener)
        {
            if (m_updateListeners.Contains(p_listener))
                return;
            
            m_updateListeners.Add(p_listener);
        }

        public void AddFixedUpdateListener(IFixedUpdateListener p_listener)
        {
            if (m_fixedUpdateListeners.Contains(p_listener))
                return;
            
            m_fixedUpdateListeners.Add(p_listener);
        }

        public void AddLateUpdateListener(ILateUpdateListener p_listener)
        {
            if (m_lateUpdateListeners.Contains(p_listener))
                return;
            
            m_lateUpdateListeners.Add(p_listener);
        }

        public void RemoveUpdateListener(IUpdateListener p_listener) => m_updateListeners.Remove(p_listener);

        public void RemoveFixedUpdateListener(IFixedUpdateListener p_listener) => m_fixedUpdateListeners.Remove(p_listener);

        public void RemoveLateUpdateListener(ILateUpdateListener p_listener) => m_lateUpdateListeners.Remove(p_listener);

        public void MyUpdate()
        {
            for (var l_i = 0; l_i < m_updateListeners.Count; l_i++)
            {
                m_updateListeners[l_i].MyUpdate();
            }
        }

        public void MyFixedUpdate()
        {
            for (var l_i = 0; l_i < m_fixedUpdateListeners.Count; l_i++)
            {
                m_fixedUpdateListeners[l_i].MyFixedUpdate();
            }
        }

        public void MyLateUpdate()
        {
            for (var l_i = 0; l_i < m_lateUpdateListeners.Count; l_i++)
            {
                m_lateUpdateListeners[l_i].MyLateUpdate();
            }
        }
    }
}