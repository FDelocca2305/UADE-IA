using Services;
using Services.MicroServices.UserDataService.Wallet;
using UnityEngine;
using UnityEngine.Assertions;

namespace _2._Scripts.UI.MainMenu
{
    public class MainMenuPresenter : UIPresenter
    {
        [SerializeField] private string playUIName;
        [SerializeField] private string loadoutUIName;
        [SerializeField] private string shopUIName;
        [SerializeField] private string settingsUIName;
        
        private MainMenuModel m_mainModel;
        private MainMenuView m_mainView;
        private IWalletService m_walletService;
        
        public override void Initialize()
        {
            base.Initialize();
            m_mainModel = uiModel as MainMenuModel;
            Assert.IsNotNull(m_mainModel);

            m_mainModel.OnChangedCoins += OnChangedCoinsHandler;
            m_mainModel.OnChangedDiamon += OnChangedDiamondHandler;
            
            m_mainView = uiView as MainMenuView;
            Assert.IsNotNull(m_mainView);
            
            m_mainView.OnPlayClicked += OnPlayClickedHandler;
            m_mainView.OnLoadoutClicked += OnLoadoutClickedHandler;
            m_mainView.OnShopClicked += OnShopClickedHandler;
            m_mainView.OnSettingsClicked += OnSettingsClickedHandler;

            m_walletService = ServiceLocator.Get<IWalletService>();
            m_walletService.OnWalletChanged += OnWalletChangedHandler;
            OnWalletChangedHandler(m_walletService.Coins, m_walletService.Diamonds);
        }
        
        public override void Shutdown()
        {
            base.Shutdown();
            
            m_mainModel.OnChangedCoins -= OnChangedCoinsHandler;
            m_mainModel.OnChangedDiamon -= OnChangedDiamondHandler;
            
            m_mainView.OnPlayClicked -= OnPlayClickedHandler;
            m_mainView.OnLoadoutClicked -= OnLoadoutClickedHandler;
            m_mainView.OnShopClicked -= OnShopClickedHandler;
            m_mainView.OnSettingsClicked -= OnSettingsClickedHandler;

            if (m_walletService != null)
            {
                m_walletService.OnWalletChanged -= OnWalletChangedHandler;
            }
        }

        private void OnChangedCoinsHandler(int p_coins)
        {
            m_mainView.SetCoins(p_coins);
        }
        
        private void OnChangedDiamondHandler(int p_diamonds)
        {
            m_mainView.SetDiamonds(p_diamonds);
        }

        private void OnPlayClickedHandler()
        {
            Hide();
            panelsController.ShowUI(playUIName);
        }
        
        private void OnLoadoutClickedHandler()
        {
            Hide();
            panelsController.ShowUI(loadoutUIName);
        }
        
        private void OnShopClickedHandler()
        {
            if (UGS_Analytics.Instance != null)
            {
                UGS_Analytics.Instance.LogShopOpened(nameof(MainMenuView));
            }

            Hide();
            panelsController.ShowUI(shopUIName);
        }
        
        private void OnSettingsClickedHandler()
        {
            Hide();
            panelsController.ShowUI(settingsUIName);
        }

        private void OnWalletChangedHandler(int coins, int diamonds)
        {
            m_mainModel.SetWallet(coins, diamonds);
        }
    }
}
