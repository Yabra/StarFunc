using System;
using System.Collections.Generic;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    /// <summary>
    /// Local shop contract. Backed by the bundled/cached catalog from
    /// <see cref="ContentService"/>. Phase 4.3a will introduce an online
    /// implementation that talks to <c>POST /shop/purchase</c>; the same
    /// interface fronts both.
    /// </summary>
    public interface IShopService
    {
        /// <summary>All items currently flagged <c>isAvailable</c> in the catalog.</summary>
        IReadOnlyList<ShopItemDto> GetAvailableItems();

        /// <summary>Available items filtered to a single category (Hints/Lives/Skip/Customization).</summary>
        IReadOnlyList<ShopItemDto> GetItemsByCategory(string category);

        /// <summary>
        /// Purchase by id. Returns true on success. Reasons for failure are
        /// logged: missing item, already owned (non-consumable), insufficient
        /// fragments. Successful purchase persists save and fires <see cref="OnItemPurchased"/>.
        /// </summary>
        bool PurchaseItem(string itemId);

        /// <summary>True if the player owns the (non-consumable) item.</summary>
        bool IsItemOwned(string itemId);

        /// <summary>
        /// Current count of a consumable bucket. Hint packs accumulate in <c>"hints"</c>;
        /// life-restore items in <c>"life_restores"</c>; skip-tokens in <c>"skip_tokens"</c>.
        /// </summary>
        int GetConsumableCount(string consumableKey);

        /// <summary>Fires after a successful purchase (post-save).</summary>
        event Action<ShopItemDto> OnItemPurchased;
    }
}
