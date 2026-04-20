using System.Collections.Generic;
using Game.Upgrades;
using Services;
using Services.MicroServices.UserDataService.PlayerUpgrades;
using Services.MicroServices.UserDataService.Wallet;
using UnityEngine;
using UnityEngine.Assertions;

namespace _2._Scripts.UI.MainMenu.Shop
{
    public class ShopPresenter : UIPresenter
    {
        [SerializeField] private string mainUIName = "Main";
        [SerializeField] private ShopCategory defaultCategory = ShopCategory.Weapons;
        [Header("Upgrades")]
        [SerializeField] private MainCharacterDataSO baseStats;
        [SerializeField] private float maxHealthCap = 250f;
        [SerializeField] private float moveSpeedCap = 10f;
        [SerializeField] private float magSizeCap = 50f;
        [SerializeField] private float reloadTimeMin = 0.5f;
        [SerializeField] private float shootCooldownMin = 0.05f;
        [SerializeField] private float bulletSpeedCap = 60f;
        [SerializeField] private float bulletDamageCap = 200f;

        private ShopModel m_model;
        private ShopView m_view;
        private ShopCategory m_currentCategory;
        private IWalletService m_walletService;
        private IPlayerUpgradeService m_upgradeService;
        private bool m_isVisible;
        private bool m_purchaseMadeThisSession;
        private float m_shopOpenedTime;
        private float m_lastScrollPosition;

        public override void Initialize()
        {
            base.Initialize();

            m_model = uiModel as ShopModel;
            Assert.IsNotNull(m_model, "ShopPresenter requires a ShopModel");

            m_view = uiView as ShopView;
            Assert.IsNotNull(m_view, "ShopPresenter requires a ShopView");

            m_view.OnCategorySelected += OnCategorySelectedHandler;
            m_view.OnBackClicked += OnBackClickedHandler;
            m_view.OnBuyRequested += OnBuyRequestedHandler;

            m_walletService = ServiceLocator.Get<IWalletService>();
            m_walletService.OnWalletChanged += OnWalletChangedHandler;
            OnWalletChangedHandler(m_walletService.Coins, m_walletService.Diamonds);

            m_upgradeService = ServiceLocator.Get<IPlayerUpgradeService>();

            ShowCategory(defaultCategory, 0f);

            Hide();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            m_view.OnCategorySelected -= OnCategorySelectedHandler;
            m_view.OnBackClicked -= OnBackClickedHandler;
            m_view.OnBuyRequested -= OnBuyRequestedHandler;

            if (m_walletService != null)
            {
                m_walletService.OnWalletChanged -= OnWalletChangedHandler;
            }
        }

        public override void Show()
        {
            base.Show();
            m_isVisible = true;
            m_purchaseMadeThisSession = false;
            m_shopOpenedTime = Time.timeSinceLevelLoad;
            LogCurrentCategoryView();
        }

        public override void Hide()
        {
            if (m_isVisible && !m_purchaseMadeThisSession && UGS_Analytics.Instance != null)
            {
                float duration = Mathf.Max(0f, Time.timeSinceLevelLoad - m_shopOpenedTime);
                UGS_Analytics.Instance.LogShopClosedWithoutPurchase(duration);
            }

            m_isVisible = false;
            base.Hide();
        }

        private void OnCategorySelectedHandler(ShopCategory category, float scrollPosition)
        {
            ShowCategory(category, scrollPosition);
        }

        private void ShowCategory(ShopCategory category, float scrollPosition)
        {
            m_currentCategory = category;
             m_lastScrollPosition = scrollPosition;

            IReadOnlyList<ShopModel.ShopItemDefinition> items = m_model.GetItemsForCategory(category);
            m_view.DisplayItems(items);
            m_view.ScrollTo(scrollPosition);

            if (m_isVisible && UGS_Analytics.Instance != null)
            {
                UGS_Analytics.Instance.LogShopCategoryViewed(category.ToString(), scrollPosition);
            }
        }

        private void OnBackClickedHandler()
        {
            Hide();
            panelsController.ShowUI(mainUIName);
        }

        private void OnBuyRequestedHandler(ShopModel.ShopItemDefinition item)
        {
            string itemId = string.IsNullOrEmpty(item.id) ? item.displayName : item.id;
            var analytics = UGS_Analytics.Instance;
            if (analytics != null)
            {
                analytics.LogShopPurchaseAttempt(itemId, item.currency.ToString(), item.price);
            }

            if (!TrySpendCurrency(item))
            {
                MyLogger.LogWarning($"Not enough {(item.currency == ShopCurrency.Coins ? "coins" : "diamonds")} to buy {item.displayName}");
                return;
            }

            m_purchaseMadeThisSession = true;

            if (analytics != null)
            {
                analytics.LogShopPurchaseSuccess(itemId, item.currency.ToString(), item.price);
            }

            ApplyUpgrade(item);
        }

        private bool TrySpendCurrency(ShopModel.ShopItemDefinition item)
        {
            return item.currency switch
            {
                ShopCurrency.Coins => m_walletService?.SpendCoins(item.price) ?? false,
                ShopCurrency.Diamonds => m_walletService?.SpendDiamonds(item.price) ?? false,
                _ => false
            };
        }

        private void ApplyUpgrade(ShopModel.ShopItemDefinition item)
        {
            if (item.upgradeType == PlayerUpgradeType.None)
            {
                MyLogger.LogInfo($"Purchased {item.displayName} (no stat change configured).");
                return;
            }

            if (m_upgradeService == null)
            {
                MyLogger.LogWarning("PlayerUpgradeService unavailable. Upgrade ignored.");
                return;
            }

            float delta = CalculateAllowedDelta(item.upgradeType, item.upgradeValue);
            if (delta <= 0f)
            {
                MyLogger.LogInfo($"{item.upgradeType} already at cap.");
                return;
            }

            m_upgradeService.ApplyUpgrade(item.upgradeType, delta);
            MyLogger.LogInfo($"Applied {item.upgradeType} upgrade (+{delta}).");
        }

        private void OnWalletChangedHandler(int coins, int diamonds)
        {
            m_view.SetCurrency(coins, diamonds);
        }

        private void LogCurrentCategoryView()
        {
            if (UGS_Analytics.Instance != null)
            {
                UGS_Analytics.Instance.LogShopCategoryViewed(m_currentCategory.ToString(), m_lastScrollPosition);
            }
        }

        private float CalculateAllowedDelta(PlayerUpgradeType type, float requestedDelta)
        {
            if (baseStats == null)
            {
                return requestedDelta;
            }

            var state = m_upgradeService?.State;
            float bonus = state == null ? 0f : type switch
            {
                PlayerUpgradeType.MaxHealth => state.maxHealthBonus,
                PlayerUpgradeType.MoveSpeed => state.moveSpeedBonus,
                PlayerUpgradeType.ShootCooldown => state.shootCooldownReduction,
                PlayerUpgradeType.MagSize => state.magSizeBonus,
                PlayerUpgradeType.ReloadTime => state.reloadTimeReduction,
                PlayerUpgradeType.BulletSpeed => state.bulletSpeedBonus,
                PlayerUpgradeType.BulletDamage => state.bulletDamageBonus,
                _ => 0f
            };

            switch (type)
            {
                case PlayerUpgradeType.MaxHealth:
                    float currentHealth = baseStats.maxHealth + bonus;
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, maxHealthCap - currentHealth));
                case PlayerUpgradeType.MoveSpeed:
                    float currentSpeed = baseStats.moveSpeed + bonus;
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, moveSpeedCap - currentSpeed));
                case PlayerUpgradeType.ShootCooldown:
                    float currentCooldown = Mathf.Max(shootCooldownMin, baseStats.shootCooldown - bonus);
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, currentCooldown - shootCooldownMin));
                case PlayerUpgradeType.MagSize:
                    float currentMag = baseStats.magSize + bonus;
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, magSizeCap - currentMag));
                case PlayerUpgradeType.ReloadTime:
                    float currentReload = Mathf.Max(reloadTimeMin, baseStats.reloadTime - bonus);
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, currentReload - reloadTimeMin));
                case PlayerUpgradeType.BulletSpeed:
                    if (baseStats.bulletData == null) return 0f;
                    float currentBulletSpeed = baseStats.bulletData.Speed + bonus;
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, bulletSpeedCap - currentBulletSpeed));
                case PlayerUpgradeType.BulletDamage:
                    if (baseStats.bulletData == null) return 0f;
                    float currentDamage = baseStats.bulletData.Damage + bonus;
                    return Mathf.Max(0f, Mathf.Min(requestedDelta, bulletDamageCap - currentDamage));
                default:
                    return requestedDelta;
            }
        }
    }
}
