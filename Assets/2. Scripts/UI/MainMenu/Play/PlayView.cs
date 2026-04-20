using System;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.MainMenu.Play
{
    public class PlayView : UIView
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button startButtonAlt;
        [SerializeField] private Button backButton;

        public event Action OnStartClicked;
        public event Action OnStartAltClicked;
        public event Action OnBackClicked;
        
        public override void Show()
        {
            base.Show();
            startButton.onClick.AddListener(OnStartButtonHandler);
            startButtonAlt.onClick.AddListener(OnStartAltButtonHandler);
            backButton.onClick.AddListener(OnBackButtonHandler);
        }

        public override void Hide()
        {
            base.Hide();
            startButton.onClick.RemoveListener(OnStartButtonHandler);
            startButtonAlt.onClick.RemoveListener(OnStartAltButtonHandler);
            backButton.onClick.RemoveListener(OnBackButtonHandler);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            startButton.onClick.RemoveListener(OnStartButtonHandler);
            startButtonAlt.onClick.RemoveListener(OnStartAltButtonHandler);
            backButton.onClick.RemoveListener(OnBackButtonHandler);
        }

        private void OnStartButtonHandler()
        {
            OnStartClicked?.Invoke();
        }

        private void OnStartAltButtonHandler()
        {
            OnStartAltClicked?.Invoke();
        }
        
        private void OnBackButtonHandler()
        {
            OnBackClicked?.Invoke();
        }
    }
}
