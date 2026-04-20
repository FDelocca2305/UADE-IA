using System;
using Services;
using Services.MicroServices.AudioService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.Gameplay.Results
{
    public class ResultsView : UIView
    {
        [Header("Texts")] [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text coinsRewardText;
        [SerializeField] private TMP_Text diamondsRewardText;

        [Header("Buttons")] [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button rewardedAdButton;

        [Header("Copy")] [SerializeField] private string victoryTitle = "Victoria";
        [SerializeField] private string defeatTitle = "Derrota";
        [SerializeField] private string scoreFormat = "Puntaje: {0}";
        [SerializeField] private string coinsFormat = "+{0} Coins";
        [SerializeField] private string diamondsFormat = "+{0} Diamonds";
        [SerializeField, TextArea] private string victoryDetails = "Excelente trabajo.";
        [SerializeField, TextArea] private string defeatDetails = "Intentá nuevamente.";

        public event Action OnRetry;
        public event Action OnMainMenu;
        public event Action OnRewardedAd;

        private static AudioService AudioService => AudioService.Instance;
        private AudioConfig m_audioConfig;

        private bool showRewardedAd; // bool to know if rewardedAdButton has to be show by score
        private bool rewardedAdShowed = false; // bool to know if rewarded ad has already been shown

        public bool RewardedAdShowed
        {
            set
            {
                rewardedAdShowed = value;
                rewardedAdButton.gameObject.SetActive(!value);
            }
        }

        private void Awake()
        {
            m_audioConfig = AudioService.GetConfig();
        }

        public void DisplayVictory(int score, int coins, int diamonds)
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.winSFX);
            }

            SetTexts(victoryTitle, victoryDetails, score, coins, diamonds);
        }

        public void DisplayDefeat(int score, int coins, int diamonds)
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.lostSFX);
            }

            SetTexts(defeatTitle, defeatDetails, score, coins, diamonds);
        }

        private void SetTexts(string title, string details, int score, int coins, int diamonds)
        {
            if (titleText)
            {
                titleText.text = title;
            }

            if (detailsText)
            {
                detailsText.text = details;
            }

            if (scoreText)
            {
                scoreText.text = string.Format(scoreFormat, score);
            }

            if (coinsRewardText)
            {
                coinsRewardText.text = string.Format(coinsFormat, coins);
            }

            if (diamondsRewardText)
            {
                diamondsRewardText.text = string.Format(diamondsFormat, diamonds);
            }
            
            bool hasPoints = (diamonds != 0 || coins != 0 || score != 0);
            
            showRewardedAd = hasPoints && !rewardedAdShowed;
        }

        public override void Show()
        {
            base.Show();
            if (retryButton)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (mainMenuButton)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (rewardedAdButton)
            {
                rewardedAdButton.onClick.AddListener(OnRewardedAdClicked);
            }
        
            if (!showRewardedAd)
            {
                rewardedAdButton.gameObject.SetActive(false);
                rewardedAdButton.onClick.RemoveListener(OnRewardedAdClicked);
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (retryButton)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (mainMenuButton)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
            
            if (rewardedAdButton)
            {
                rewardedAdButton.onClick.RemoveListener(OnRewardedAdClicked);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            if (retryButton)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (mainMenuButton)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
            
            if (rewardedAdButton)
            {
                rewardedAdButton.onClick.RemoveListener(OnRewardedAdClicked);
            }
        }

        private void OnRetryClicked()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnRetry?.Invoke();
        }

        private void OnMainMenuClicked()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnMainMenu?.Invoke();
        }
        
        private void OnRewardedAdClicked()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }
            
            OnRewardedAd?.Invoke();
        }

    }
}
