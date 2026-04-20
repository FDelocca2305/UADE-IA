using System;
using System.Collections.Generic;
using Services;
using Services.MicroServices.AudioService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.MainMenu.Shop
{
    public class ShopView : UIView
    {
        [Serializable]
        private struct CategoryTab
        {
            public ShopCategory category;
            public Button button;
            [Range(0f, 1f)] public float scrollPosition;
        }

        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text diamondsText;
        [SerializeField] private Button backButton;
        [SerializeField] private ScrollRect carouselScrollRect;
        [SerializeField] private ShopItemSlot[] itemSlots;
        [SerializeField] private CategoryTab[] categoryTabs;

        public event Action<ShopCategory, float> OnCategorySelected;
        public event Action OnBackClicked;
        public event Action<ShopModel.ShopItemDefinition> OnBuyRequested;

        private static AudioService AudioService => AudioService.Instance;
        private AudioConfig m_audioConfig;

        private void Awake()
        {
            m_audioConfig = AudioService.GetConfig();

            for (var i = 0; i < itemSlots.Length; i++)
            {
                if (!itemSlots[i]) continue;
                itemSlots[i].OnBuyClicked += HandleBuyClicked;
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < itemSlots.Length; i++)
            {
                if (!itemSlots[i]) continue;
                itemSlots[i].OnBuyClicked -= HandleBuyClicked;
            }
        }

        public override void Show()
        {
            base.Show();

            // Play title background music in shop
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlayMusic(m_audioConfig.titleBackground);
            }

            if (backButton)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            for (var i = 0; i < categoryTabs.Length; i++)
            {
                var tab = categoryTabs[i];
                if (tab.button == null) continue;

                var capturedTab = tab;
                tab.button.onClick.AddListener(() => HandleCategoryClicked(capturedTab));
            }
        }

        public override void Hide()
        {
            base.Hide();

            if (backButton)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }

            for (var i = 0; i < categoryTabs.Length; i++)
            {
                var tab = categoryTabs[i];
                if (tab.button == null) continue;
                tab.button.onClick.RemoveAllListeners();
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            OnCategorySelected = null;
            OnBackClicked = null;
            OnBuyRequested = null;
        }

        public void SetCurrency(int coins, int diamonds)
        {
            if (coinsText)
            {
                coinsText.text = coins.ToString();
            }

            if (diamondsText)
            {
                diamondsText.text = diamonds.ToString();
            }
        }

        public void DisplayItems(IReadOnlyList<ShopModel.ShopItemDefinition> items)
        {
            for (var i = 0; i < itemSlots.Length; i++)
            {
                if (i < items.Count)
                {
                    itemSlots[i].SetData(items[i]);
                }
                else
                {
                    itemSlots[i].Clear();
                }
            }
        }

        public void ScrollTo(float normalizedPosition)
        {
            if (!carouselScrollRect) return;
            carouselScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        }

        private void HandleCategoryClicked(CategoryTab tab)
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnCategorySelected?.Invoke(tab.category, tab.scrollPosition);
        }

        private void HandleBackClicked()
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.clickButtonSFX);
            }

            OnBackClicked?.Invoke();
        }

        private void HandleBuyClicked(ShopModel.ShopItemDefinition item)
        {
            if (AudioService != null && m_audioConfig != null)
            {
                AudioService.PlaySFX(m_audioConfig.cashRegisterSFX);
            }

            OnBuyRequested?.Invoke(item);
        }
    }
}
