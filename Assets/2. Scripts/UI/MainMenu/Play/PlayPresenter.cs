using Services;
using Services.MicroServices.GameStateService;
using UnityEngine.Assertions;
using UnityEngine;

namespace _2._Scripts.UI.MainMenu.Play
{
    public class PlayPresenter : UIPresenter
    {
        [SerializeField] private string mainUIName = "Main";
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private string gameplaySceneNameAlt = "Gameplay2";
        [SerializeField] private string loadingSceneName = "Loading";
        
        private IGameStateService m_gameStateService;
        private PlayView m_playView;

        public override void Initialize()
        {
            base.Initialize();
            m_playView = uiView as PlayView;
            Assert.IsNotNull(m_playView);
            
            m_gameStateService = ServiceLocator.Get<IGameStateService>();
        }

        public override void Show()
        {
            base.Show();
            m_playView.OnStartClicked += OnStartClickedHandler;
            m_playView.OnStartAltClicked += OnStartAltClickedHandler;
            m_playView.OnBackClicked += OnBackClickedHandler;
        }

        public override void Hide()
        {
            base.Hide();
            m_playView.OnStartClicked -= OnStartClickedHandler;
            m_playView.OnStartAltClicked -= OnStartAltClickedHandler;
            m_playView.OnBackClicked -= OnBackClickedHandler;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            m_playView.OnStartClicked -= OnStartClickedHandler;
            m_playView.OnStartAltClicked -= OnStartAltClickedHandler;
            m_playView.OnBackClicked -= OnBackClickedHandler;
        }

        private void OnStartClickedHandler()
        {
            m_gameStateService?.ChangeState(GameState.Playing);
            SceneLoadManager.LoadWithLoading(gameplaySceneName, loadingSceneName);
        }

        private void OnStartAltClickedHandler()
        {
            m_gameStateService?.ChangeState(GameState.Playing);
            SceneLoadManager.LoadWithLoading(gameplaySceneNameAlt, loadingSceneName);
        }
        
        private void OnBackClickedHandler()
        {
            Hide();
            panelsController.ShowUI(mainUIName);
        }
    }
}
