using System;
using System.Collections.Generic;
using System.Linq;
using Game.Upgrades;
using UnityEngine;

namespace _2._Scripts.UI.MainMenu.Shop
{
    public class ShopModel : UIModel
    {
        [Serializable]
        public class ShopItemDefinition
        {
            public string id;
            public string displayName;
            public Sprite icon;
            public int price;
            public ShopCurrency currency;
            public ShopCategory category;
            public bool showSaleBadge;
            [Tooltip("Used to define the order inside the carousel.")]
            public int order;
            [Header("Upgrade Effect")]
            public PlayerUpgradeType upgradeType;
            public float upgradeValue;
        }

        [SerializeField] private List<ShopItemDefinition> placeholderItems = new();

        private readonly Dictionary<ShopCategory, List<ShopItemDefinition>> m_itemsByCategory = new();

        public IReadOnlyList<ShopItemDefinition> GetItemsForCategory(ShopCategory category)
        {
            if (m_itemsByCategory.TryGetValue(category, out var cached))
            {
                return cached;
            }

            var orderedItems = placeholderItems
                .Where(item => item.category == category)
                .OrderBy(item => item.order)
                .ToList();

            m_itemsByCategory[category] = orderedItems;
            return orderedItems;
        }

        public IReadOnlyList<ShopItemDefinition> GetAllItemsOrdered()
        {
            return placeholderItems
                .OrderBy(item => item.order)
                .ToList();
        }
    }
}
