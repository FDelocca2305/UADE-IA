using UnityEngine;

namespace _2._Scripts.UI
{
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup mainPanel;
        
        private UIPresenter m_presenter;
        
        public virtual void Initialize(UIPresenter p_presenter)
        {
            m_presenter = p_presenter;
        }
        
        public virtual void Shutdown()
        {
            
        }
        
        public virtual void Show()
        {
            mainPanel.interactable = true;
            mainPanel.blocksRaycasts = true;
            mainPanel.alpha = 1;
        }
        
        public virtual void Hide()
        {
            mainPanel.interactable = false;
            mainPanel.blocksRaycasts = false;
            mainPanel.alpha = 0;
        }
    }
}
