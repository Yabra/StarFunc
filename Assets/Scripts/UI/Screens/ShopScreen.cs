using System;
using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Tabbed shop screen: category buttons + a list of item widgets for the
    /// selected category. Pulls items via <see cref="IShopService"/> and
    /// rebuilds when an item is purchased so the new "Owned" / consumable
    /// counts show up immediately.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        [Header("Item List")]
        [SerializeField] Transform _itemContainer;
        [SerializeField] ShopItemWidget _itemWidgetPrefab;

        [Header("Top Bar")]
        [SerializeField] FragmentsDisplay _fragmentsDisplay;
        [SerializeField] Button _closeButton;

        [Header("Category Tabs")]
        [SerializeField] CategoryTab[] _categoryTabs;
        [SerializeField] string _defaultCategory = "Hints";

        IShopService _shop;
        IEconomyService _economy;
        IUIService _ui;
        string _currentCategory;
        readonly List<ShopItemWidget> _activeWidgets = new();

        void Awake()
        {
            if (_closeButton) _closeButton.onClick.AddListener(OnCloseClicked);

            if (_categoryTabs != null)
            {
                foreach (var tab in _categoryTabs)
                {
                    if (tab.Button == null) continue;
                    var captured = tab.CategoryId;
                    tab.Button.onClick.AddListener(() => SelectCategory(captured));
                }
            }
        }

        void OnDestroy()
        {
            if (_closeButton) _closeButton.onClick.RemoveListener(OnCloseClicked);
            UnsubscribeShop();
        }

        public override void Show()
        {
            base.Show();
            ResolveServices();
            SubscribeShop();
            SelectCategory(string.IsNullOrEmpty(_currentCategory) ? _defaultCategory : _currentCategory);
        }

        public override void Hide()
        {
            UnsubscribeShop();
            ClearWidgets();
            base.Hide();
        }

        // =========================================================================
        // Categories
        // =========================================================================

        void SelectCategory(string category)
        {
            _currentCategory = category;
            UpdateCategoryHighlights();
            Refresh();
        }

        void UpdateCategoryHighlights()
        {
            if (_categoryTabs == null) return;
            foreach (var tab in _categoryTabs)
            {
                if (tab.HighlightTarget == null) continue;
                bool selected = string.Equals(tab.CategoryId, _currentCategory,
                                              StringComparison.OrdinalIgnoreCase);
                tab.HighlightTarget.SetActive(selected);
            }
        }

        // =========================================================================
        // Items
        // =========================================================================

        void Refresh()
        {
            ClearWidgets();
            if (_shop == null || _itemWidgetPrefab == null || _itemContainer == null)
                return;

            var items = _shop.GetItemsByCategory(_currentCategory);
            for (int i = 0; i < items.Count; i++)
            {
                var widget = Instantiate(_itemWidgetPrefab, _itemContainer);
                widget.gameObject.SetActive(true);
                widget.Setup(items[i], _shop);
                _activeWidgets.Add(widget);
            }

            if (_fragmentsDisplay && _economy != null)
                _fragmentsDisplay.SetFragments(_economy.GetFragments());
        }

        void ClearWidgets()
        {
            foreach (var widget in _activeWidgets)
                if (widget) Destroy(widget.gameObject);
            _activeWidgets.Clear();
        }

        // =========================================================================
        // Service plumbing
        // =========================================================================

        void ResolveServices()
        {
            if (_shop == null && ServiceLocator.Contains<IShopService>())
                _shop = ServiceLocator.Get<IShopService>();
            if (_economy == null && ServiceLocator.Contains<IEconomyService>())
                _economy = ServiceLocator.Get<IEconomyService>();
            if (_ui == null && ServiceLocator.Contains<IUIService>())
                _ui = ServiceLocator.Get<IUIService>();
        }

        void SubscribeShop()
        {
            if (_shop == null) return;
            _shop.OnItemPurchased -= OnItemPurchased;
            _shop.OnItemPurchased += OnItemPurchased;
        }

        void UnsubscribeShop()
        {
            if (_shop == null) return;
            _shop.OnItemPurchased -= OnItemPurchased;
        }

        void OnItemPurchased(ShopItemDto _) => Refresh();

        void OnCloseClicked()
        {
            if (_ui != null) _ui.HideScreen<ShopScreen>();
            else Hide();
        }

        // =========================================================================
        // Inspector helper
        // =========================================================================

        [Serializable]
        public class CategoryTab
        {
            public string CategoryId;
            public Button Button;
            public GameObject HighlightTarget;
        }
    }
}
