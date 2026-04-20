using _2._Scripts.UI.MainMenu;
using UnityEngine;

namespace _2._Scripts.UI
{
    public abstract class UIPresenter : MonoBehaviour
    {
        [field: SerializeField] public string UIName { get; private set; }
        [SerializeField] protected PanelsController panelsController;
        [SerializeField] protected UIModel uiModel;
        [SerializeField] protected UIView uiView;

        public virtual void Initialize()
        {
            uiModel.Initialize(this);
            uiView.Initialize(this);
        }

        public virtual void Shutdown()
        {
            uiModel.Shutdown();
            uiView.Shutdown();
        }

        public virtual void Show()
        {
            uiView.Show();
        }

        public virtual void Hide()
        {
            uiView.Hide();
        }
    }
}