using UnityEngine;

namespace _2._Scripts.UI
{
    public abstract class UIModel : MonoBehaviour
    {
        private UIPresenter m_presenter;
        
        public virtual void Initialize(UIPresenter p_presenter)
        {
            m_presenter = p_presenter;
        }
        
        public virtual void Shutdown()
        {
            
        }
    }
}