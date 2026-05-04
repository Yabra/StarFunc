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
    /// <see cref="IShopService.PurchaseItem"/>.
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

        void Awake()
        {
            if (_buyButton) _buyButton.onClick.AddListener(OnBuyClicked);
        }

        void OnDestroy()
        {
            if (_buyButton) _buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        public void Setup(ShopItemDto item, IShopService shop)
        {
            _item = item;
            _shop = shop;

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
            _shop?.PurchaseItem(_item.ItemId);
            // ShopScreen subscribes to OnItemPurchased and refreshes the list,
            // which will rebuild this widget — no need to re-Setup here.
        }
    }
}
