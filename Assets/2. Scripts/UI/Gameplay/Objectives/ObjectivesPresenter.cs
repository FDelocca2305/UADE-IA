using Services;
using Services.MicroServices.GameStateService;
using UnityEngine;
using UnityEngine.Assertions;

namespace _2._Scripts.UI.Gameplay.Objectives
{
    public class ObjectivesPresenter : UIPresenter
    {
        [SerializeField] private string uiTitle = "Objetivos";
        [SerializeField, TextArea] private string[] objectives;
        [SerializeField] private bool pauseGameWhileVisible = true;
        [SerializeField] private bool showOnStart = false;
        
        private ObjectivesModel m_model;
        private ObjectivesView m_view;
        private IGameStateService m_gameStateService;
        private float m_previousTimeScale = 1f;
        private bool m_isVisible;

        public override void Initialize()
        {
            base.Initialize();

            m_model = uiModel as ObjectivesModel;
            Assert.IsNotNull(m_model, "ObjectivesPresenter requires an ObjectivesModel");

            m_view = uiView as ObjectivesView;
            Assert.IsNotNull(m_view, "ObjectivesPresenter requires an ObjectivesView");

            m_gameStateService = ServiceLocator.Get<IGameStateService>();

            m_model.SetObjectives(objectives);
            m_view.SetTitle(uiTitle);
            m_view.SetObjectives(m_model.Objectives);
            
            Hide();

            if (showOnStart) Show();
        }

        public override void Show()
        {
            MyLogger.LogInfo("SE MUESTRAN LOS OBJETIVOS");
            base.Show();

            if (pauseGameWhileVisible)
            {
                m_previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            m_isVisible = true;
            m_view.OnDismissRequested += OnDismissRequested;
        }

        public override void Hide()
        {
            if (!m_isVisible)
            {
                base.Hide();
                return;
            }

            m_isVisible = false;
            m_view.OnDismissRequested -= OnDismissRequested;

            base.Hide();

            if (pauseGameWhileVisible)
            {
                Time.timeScale = m_previousTimeScale;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            m_view.OnDismissRequested -= OnDismissRequested;
        }

        private void OnDismissRequested()
        {
            Hide();
            m_gameStateService?.ChangeState(GameState.Playing);
        }
    }
}
