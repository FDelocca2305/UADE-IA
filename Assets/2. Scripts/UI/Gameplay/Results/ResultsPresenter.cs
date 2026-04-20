using System;
using Services;
using Services.MicroServices.AdsService;
using Services.MicroServices.EventsServices;
using Services.MicroServices.EventsServices.CustomEvents;
using Services.MicroServices.GameStateService;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace _2._Scripts.UI.Gameplay.Results
{
    public class ResultsPresenter : UIPresenter
    {
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private bool pauseGameWhileVisible = true;
        [SerializeField] private PlayerCollector playerCollector;

        private ResultsModel m_model;
        private ResultsView m_view;
        private IEventService m_eventService;
        private IGameStateService m_gameStateService;
        private float m_previousTimeScale = 1f;
        private bool m_isVisible;

        public override void Initialize()
        {
            base.Initialize();

            m_model = uiModel as ResultsModel;
            Assert.IsNotNull(m_model, "ResultsPresenter requires a ResultsModel");

            m_view = uiView as ResultsView;
            Assert.IsNotNull(m_view, "ResultsPresenter requires a ResultsView");

            m_eventService = ServiceLocator.Get<IEventService>();
            m_gameStateService = ServiceLocator.Get<IGameStateService>();

            m_eventService.AddListener<GameResultEvent>(OnGameResultEvent);
            m_gameStateService.OnStateChanged += OnGameStateChanged;

            Hide();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            if (m_eventService != null)
            {
                m_eventService.RemoveListener<GameResultEvent>(OnGameResultEvent);
            }

            if (m_gameStateService != null)
            {
                m_gameStateService.OnStateChanged -= OnGameStateChanged;
            }

            m_view.OnRetry -= OnRetryPressed;
            m_view.OnMainMenu -= OnMainMenuPressed;
        }

        private void OnGameResultEvent(GameResultEvent resultEvent)
        {
            m_model.SetResult(resultEvent);

            if (UGS_Analytics.Instance != null)
            {
                UGS_Analytics.Instance.LogSessionCompleted(resultEvent.IsVictory, resultEvent.Score, Time.timeSinceLevelLoad);
            }

            if (resultEvent.IsVictory)
            {
                m_view.DisplayVictory(resultEvent.Score, resultEvent.EarnedCoins, resultEvent.EarnedDiamonds);
                m_gameStateService?.ChangeState(GameState.Victory);
            }
            else
            {
                m_view.DisplayDefeat(resultEvent.Score, resultEvent.EarnedCoins, resultEvent.EarnedDiamonds);
                m_gameStateService?.ChangeState(GameState.GameOver);
            }

            Show();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current is GameState.Victory or GameState.GameOver)
            {
                Show();
            }
            else if (m_isVisible)
            {
                Hide();
            }
        }

        public override void Show()
        {
            if (m_isVisible)
            {
                return;
            }

            m_isVisible = true;

            if (pauseGameWhileVisible)
            {
                m_previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            m_view.OnRetry += OnRetryPressed;
            m_view.OnMainMenu += OnMainMenuPressed;
            m_view.OnRewardedAd += OnRewardedAdPressed;

            base.Show();
        }

        public override void Hide()
        {
            if (!m_isVisible)
            {
                base.Hide();
                return;
            }

            m_isVisible = false;

            if (pauseGameWhileVisible)
            {
                Time.timeScale = m_previousTimeScale;
            }

            m_view.OnRetry -= OnRetryPressed;
            m_view.OnMainMenu -= OnMainMenuPressed;
            m_view.OnRewardedAd -= OnRewardedAdPressed;

            base.Hide();
        }

        private void OnRetryPressed()
        {
            if (UGS_Analytics.Instance != null)
            {
                var lastResult = m_model?.LastResult;
                int lastScore = lastResult?.Score ?? 0;
                bool lastWasVictory = lastResult?.IsVictory ?? false;
                UGS_Analytics.Instance.LogRetryPressed(lastScore, lastWasVictory);
            }

            Time.timeScale = 1f;
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
            m_gameStateService?.ChangeState(GameState.Playing);
        }

        private void OnMainMenuPressed()
        {
            Time.timeScale = 1f;
            m_gameStateService?.ChangeState(GameState.Menu);
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnRewardedAdPressed()
        {
            ServiceLocator.Get<IAdsService>().Show(playerCollector, m_view);
        }

    }
}
