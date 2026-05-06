using System;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// One row inside <see cref="ShopScreen"/>. Bound to a single
    /// <see cref="ShopItemDto"/>; tap on Buy goes through
    /// <see cref="IShopService.PurchaseItem"/>. If the player can't afford
    /// the item, the buy click is short-circuited and an "insufficient
    /// funds" callback is fired so the screen can shake the fragments
    /// counter (see ShopScreen).
    /// </summary>
    public class ShopItemWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text _nameText;
        [SerializeField] TMP_Text _descriptionText;
        [SerializeField] TMP_Text _priceText;
        [SerializeField] TMP_Text _ownedLabel;
        [SerializeField] Button _buyButton;
        [SerializeField] GameObject _ownedBadge;

        ShopItemDto _item;
        IShopService _shop;
        IEconomyService _economy;
        Action _onInsufficientFunds;

        void Awake()
        {
            if (_buyButton) _buyButton.onClick.AddListener(OnBuyClicked);
        }

        void OnDestroy()
        {
            if (_buyButton) _buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        public void Setup(ShopItemDto item, IShopService shop,
            IEconomyService economy = null, Action onInsufficientFunds = null)
        {
            _item = item;
            _shop = shop;
            _economy = economy;
            _onInsufficientFunds = onInsufficientFunds;

            if (_nameText) _nameText.text = item.DisplayName ?? item.ItemId;
            if (_descriptionText) _descriptionText.text = item.Description ?? string.Empty;
            if (_priceText) _priceText.text = item.Price.ToString();

            bool owned = !item.IsConsumable && shop != null && shop.IsItemOwned(item.ItemId);
            if (_ownedBadge) _ownedBadge.SetActive(owned);
            if (_ownedLabel) _ownedLabel.gameObject.SetActive(owned);
            if (_buyButton) _buyButton.interactable = !owned;
        }

        void OnBuyClicked()
        {
            // Pre-check funds so we can give a tactile "not enough" cue
            // rather than just a silent service-side rejection. The shop
            // service still re-validates internally as the source of truth.
            if (_economy != null && _item != null && !_economy.CanAfford(_item.Price))
            {
                _onInsufficientFunds?.Invoke();
                return;
            }

            _shop?.PurchaseItem(_item.ItemId);
            // ShopScreen subscribes to OnItemPurchased and refreshes the list,
            // which will rebuild this widget — no need to re-Setup here.
        }
    }
}
