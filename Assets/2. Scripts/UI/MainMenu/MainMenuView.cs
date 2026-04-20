using System;
using Services;
using Services.MicroServices.AudioService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.MainMenu
{
    public class MainMenuView : UIView
    {
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI diamondText;

        [SerializeField] private Button playButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        public event Action OnPlayClicked;
        public event Action OnLoadoutClicked;
        public event Action OnShopClicked;
        public event Action OnSettingsClicked;

        private static AudioService AudioService => AudioService.Instance;
        private AudioConfig m_audioConfig;

        private void Awake()
        {
            m_audioConfig = AudioService.GetConfig();
            
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlayMusic(m_audioConfig.titleBackground);
            }
        }

        public override void Initialize(UIPresenter p_presenter)
        {
            base.Initialize(p_presenter);
            Show();
        }

        public override void Show()
        {
            base.Show();

            playButton.onClick.AddListener(OnPlayButtonHandler);
            loadoutButton.onClick.AddListener(OnLoadoutButtonHandler);
            shopButton.onClick.AddListener(OnShopButtonHandler);
            settingsButton.onClick.AddListener(OnSettingsButtonHandler);
        }

        public override void Hide()
        {
            base.Hide();
            
            playButton.onClick.RemoveListener(OnPlayButtonHandler);
            loadoutButton.onClick.RemoveListener(OnLoadoutButtonHandler);
            shopButton.onClick.RemoveListener(OnShopButtonHandler);
            settingsButton.onClick.RemoveListener(OnSettingsButtonHandler);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            
            playButton.onClick.RemoveListener(OnPlayButtonHandler);
            loadoutButton.onClick.RemoveListener(OnLoadoutButtonHandler);
            shopButton.onClick.RemoveListener(OnShopButtonHandler);
            settingsButton.onClick.RemoveListener(OnSettingsButtonHandler);
        }

        private void OnPlayButtonHandler()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnPlayClicked?.Invoke();
        }

        private void OnLoadoutButtonHandler()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnLoadoutClicked?.Invoke();
        }

        private void OnShopButtonHandler()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnShopClicked?.Invoke();
        }

        private void OnSettingsButtonHandler()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnSettingsClicked?.Invoke();
        }
        
        public void SetCoins(int p_coins)
        {
            coinsText.text = p_coins.ToString();
        }
        
        public void SetDiamonds(int p_diamonds)
        {
            diamondText.text = p_diamonds.ToString();
        }
    }
}