using System;
using System.Collections.Generic;
using System.Linq;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Offline-first shop. Reads the catalog through <see cref="ContentService"/>
    /// (which already merges remote + bundled JSON), spends via
    /// <see cref="IEconomyService"/>, and writes ownership/consumables straight
    /// into <see cref="PlayerSaveData"/>.
    ///
    /// Phase 4.3a will wrap this in a HybridShopService that proxies online
    /// purchases through <c>POST /shop/purchase</c> and queues offline ones
    /// with <c>cachedPrice</c>; the contract on <see cref="IShopService"/>
    /// stays the same.
    /// </summary>
    public class LocalShopService : IShopService
    {
        public const string HintsKey = "hints";
        public const string LifeRestoresKey = "life_restores";
        public const string SkipTokensKey = "skip_tokens";

        readonly ContentService _content;
        readonly IEconomyService _economy;
        readonly ISaveService _saveService;
        readonly PlayerSaveData _save;

        public event Action<ShopItemDto> OnItemPurchased;

        public LocalShopService(
            ContentService content,
            IEconomyService economy,
            ISaveService saveService)
        {
            _content = content;
            _economy = economy;
            _saveService = saveService;
            _save = _saveService.Load() ?? new PlayerSaveData();
            _save.OwnedItems ??= new List<string>();
            _save.Consumables ??= new Dictionary<string, int>();
        }

        public IReadOnlyList<ShopItemDto> GetAvailableItems()
        {
            var catalog = _content?.ShopCatalog;
            if (catalog == null || catalog.Length == 0)
                return Array.Empty<ShopItemDto>();

            return catalog.Where(i => i != null && i.IsAvailable).ToArray();
        }

        public IReadOnlyList<ShopItemDto> GetItemsByCategory(string category)
        {
            var catalog = _content?.ShopCatalog;
            if (catalog == null || catalog.Length == 0 || string.IsNullOrEmpty(category))
                return Array.Empty<ShopItemDto>();

            return catalog
                .Where(i => i != null && i.IsAvailable
                            && string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public bool PurchaseItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            var catalog = _content?.ShopCatalog;
            var item = catalog?.FirstOrDefault(i => i != null && i.ItemId == itemId);
            if (item == null)
            {
                Debug.LogWarning($"[LocalShopService] Item not in catalog: {itemId}");
                return false;
            }

            if (!item.IsAvailable)
            {
                Debug.LogWarning($"[LocalShopService] Item not available: {itemId}");
                return false;
            }

            if (!item.IsConsumable && IsItemOwned(itemId))
            {
                Debug.Log($"[LocalShopService] Already owned: {itemId}");
                return false;
            }

            if (_economy == null || !_economy.SpendFragments(item.Price))
            {
                Debug.Log($"[LocalShopService] Insufficient fragments for {itemId} (price={item.Price}).");
                return false;
            }

            if (item.IsConsumable)
            {
                string key = ResolveConsumableKey(item);
                int qty = item.Quantity ?? 1;
                _save.Consumables.TryGetValue(key, out int existing);
                _save.Consumables[key] = existing + qty;
            }
            else
            {
                if (!_save.OwnedItems.Contains(itemId))
                    _save.OwnedItems.Add(itemId);
            }

            _save.IncrementVersion();
            _saveService.Save(_save);

            Debug.Log($"[LocalShopService] Purchased '{itemId}' for {item.Price} fragments " +
                      $"(consumable={item.IsConsumable}, qty={item.Quantity}).");

            if (ServiceLocator.Contains<IAnalyticsService>())
            {
                ServiceLocator.Get<IAnalyticsService>().TrackEvent(
                    AnalyticsEventNames.Purchase,
                    new Dictionary<string, object>
                    {
                        ["itemId"] = item.ItemId,
                        ["cost"] = item.Price,
                        ["currency"] = "fragments"
                    });
            }

            OnItemPurchased?.Invoke(item);
            return true;
        }

        public bool IsItemOwned(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _save.OwnedItems == null) return false;
            return _save.OwnedItems.Contains(itemId);
        }

        /// <summary>
        /// Apply a server-authoritative purchase response (task 4.3a). Used by
        /// <c>HybridShopService</c> after a successful <c>POST /shop/purchase</c>:
        /// the server has already debited fragments and computed the new
        /// consumable counts, so we just write its values into the save and
        /// fire <see cref="OnItemPurchased"/>. <paramref name="consumablesUpdate"/>
        /// keys are absolute counts (not deltas), per API.md §6.5.
        /// </summary>
        public void ApplyServerPurchaseResult(
            ShopItemDto item, IReadOnlyDictionary<string, int> consumablesUpdate)
        {
            if (item == null) return;

            if (!item.IsConsumable && !_save.OwnedItems.Contains(item.ItemId))
                _save.OwnedItems.Add(item.ItemId);

            if (consumablesUpdate != null)
            {
                foreach (var kv in consumablesUpdate)
                    _save.Consumables[kv.Key] = kv.Value;
            }

            _save.IncrementVersion();
            _saveService.Save(_save);

            OnItemPurchased?.Invoke(item);
        }

        public int GetConsumableCount(string consumableKey)
        {
            if (string.IsNullOrEmpty(consumableKey) || _save.Consumables == null) return 0;
            return _save.Consumables.TryGetValue(consumableKey, out int n) ? n : 0;
        }

        /// <summary>
        /// Map item → consumable bucket key. Hint packs all merge into "hints"
        /// so existing systems (HintSystem reads <c>Consumables["hints"]</c>)
        /// keep working without a migration.
        /// </summary>
        static string ResolveConsumableKey(ShopItemDto item) => item.Category switch
        {
            "Hints" => HintsKey,
            "Lives" => LifeRestoresKey,
            "Skip" => SkipTokensKey,
            _ => item.ItemId
        };
    }
}
