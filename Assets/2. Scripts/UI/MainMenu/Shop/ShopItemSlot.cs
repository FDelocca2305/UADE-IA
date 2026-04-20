using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.MainMenu.Shop
{
    public class ShopItemSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private Image itemIcon;
        [SerializeField] private GameObject saleBadge;
        [SerializeField] private Button buyButton;
        [SerializeField] private Sprite coinsSprite;
        [SerializeField] private Sprite diamondsSprite;

        private ShopModel.ShopItemDefinition m_currentItem;

        public event Action<ShopModel.ShopItemDefinition> OnBuyClicked;

        private void Awake()
        {
            if (buyButton)
            {
                buyButton.onClick.AddListener(HandleBuyClicked);
            }
        }

        private void OnDestroy()
        {
            if (buyButton)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
            }
        }

        public void SetData(ShopModel.ShopItemDefinition item)
        {
            m_currentItem = item;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (nameText)
            {
                nameText.text = item.displayName;
            }

            if (priceText)
            {
                priceText.text = item.price.ToString();
            }

            if (itemIcon)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = item.icon;
            }

            if (currencyIcon)
            {
                currencyIcon.sprite = item.currency == ShopCurrency.Coins ? coinsSprite : diamondsSprite;
                currencyIcon.enabled = currencyIcon.sprite != null;
            }

            if (saleBadge)
            {
                saleBadge.SetActive(item.showSaleBadge);
            }

            if (buyButton)
            {
                buyButton.interactable = true;
            }
        }

        public void Clear()
        {
            m_currentItem = null;

            if (nameText)
            {
                nameText.text = string.Empty;
            }

            if (priceText)
            {
                priceText.text = string.Empty;
            }

            if (itemIcon)
            {
                itemIcon.enabled = false;
            }

            if (currencyIcon)
            {
                currencyIcon.enabled = false;
            }

            if (saleBadge)
            {
                saleBadge.SetActive(false);
            }

            if (buyButton)
            {
                buyButton.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void HandleBuyClicked()
        {
            if (m_currentItem == null)
            {
                return;
            }

            OnBuyClicked?.Invoke(m_currentItem);
        }
    }
}
