using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarFunc.Core;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Composite shop service (API.md §6.5, §10, §11; task 4.3a).
    /// <para>
    /// • <b>Online:</b> proxies <see cref="PurchaseItem"/> through
    ///   <see cref="ServerShopService"/>. The server is authoritative —
    ///   <c>fragmentsSpent</c> / <c>newFragmentBalance</c> /
    ///   <c>consumablesUpdate</c> from the response are written into the local
    ///   save and economy via <see cref="LocalShopService.ApplyServerPurchaseResult"/>.<br/>
    /// • <b>Offline:</b> falls back to <see cref="LocalShopService"/> (local
    ///   spend + write) and enqueues a <c>POST /shop/purchase</c> with
    ///   <c>cachedPrice</c> in <see cref="SyncQueue"/>. The server honours that
    ///   price when the queue drains, even if the catalog has since shifted.<br/>
    /// • <b>Reconnect:</b> refreshes the cached shop catalog via
    ///   <see cref="ContentService.RefreshShopCatalogAsync"/> so subsequent
    ///   purchases see current prices.
    /// </para>
    /// </summary>
    public class HybridShopService : IShopService
    {
        readonly LocalShopService _local;
        readonly ServerShopService _server;
        readonly NetworkMonitor _networkMonitor;
        readonly ContentService _content;
        readonly SyncQueue _syncQueue;
        readonly IEconomyService _economy;

        public event Action<ShopItemDto> OnItemPurchased;

        public HybridShopService(
            LocalShopService local,
            ServerShopService server,
            NetworkMonitor networkMonitor,
            ContentService content,
            SyncQueue syncQueue,
            IEconomyService economy)
        {
            _local = local;
            _server = server;
            _networkMonitor = networkMonitor;
            _content = content;
            _syncQueue = syncQueue;
            _economy = economy;

            _local.OnItemPurchased += OnLocalPurchased;
            _networkMonitor.OnConnectivityChanged += OnConnectivityChanged;
        }

        public void Dispose()
        {
            _local.OnItemPurchased -= OnLocalPurchased;
            _networkMonitor.OnConnectivityChanged -= OnConnectivityChanged;
        }

        #region IShopService — read-through to local

        public IReadOnlyList<ShopItemDto> GetAvailableItems() => _local.GetAvailableItems();

        public IReadOnlyList<ShopItemDto> GetItemsByCategory(string category) =>
            _local.GetItemsByCategory(category);

        public bool IsItemOwned(string itemId) => _local.IsItemOwned(itemId);

        public int GetConsumableCount(string consumableKey) =>
            _local.GetConsumableCount(consumableKey);

        #endregion

        #region IShopService — Purchase

        /// <summary>
        /// Online → server-driven purchase, offline → local + queued retry.
        /// Returns true on local acceptance; the server may still reject a
        /// queued offline purchase later (logged + dropped from the queue by
        /// <see cref="SyncProcessor"/>).
        /// </summary>
        public bool PurchaseItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            var item = FindCatalogItem(itemId);
            if (item == null)
            {
                Debug.LogWarning($"[HybridShopService] Item not in catalog: {itemId}");
                return false;
            }

            if (!item.IsAvailable)
            {
                Debug.LogWarning($"[HybridShopService] Item not available: {itemId}");
                return false;
            }

            if (!item.IsConsumable && IsItemOwned(itemId))
            {
                Debug.Log($"[HybridShopService] Already owned: {itemId}");
                return false;
            }

            if (!_economy.CanAfford(item.Price))
            {
                Debug.Log($"[HybridShopService] Insufficient fragments for {itemId} " +
                          $"(price={item.Price}, balance={_economy.GetFragments()}).");
                return false;
            }

            if (_networkMonitor.IsOnline)
            {
                _ = PurchaseOnlineAsync(item);
                return true;
            }

            return PurchaseOffline(item);
        }

        async Task PurchaseOnlineAsync(ShopItemDto item)
        {
            try
            {
                var result = await _server.Purchase(item.ItemId, item.Price);

                if (result.WentOffline)
                {
                    // Lost connectivity mid-call — fall back to the offline path.
                    Debug.Log($"[HybridShopService] Online purchase fell back to offline " +
                              $"for '{item.ItemId}' (network dropped).");
                    PurchaseOffline(item);
                    return;
                }

                if (!result.IsSuccess || result.Data == null || !result.Data.Purchased)
                {
                    Debug.LogWarning($"[HybridShopService] Server rejected purchase '{item.ItemId}' " +
                                     $"— {result.Error?.Code}: {result.Error?.Message}");
                    return;
                }

                ApplyServerSuccess(item, result.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HybridShopService] Online purchase failed: {ex.Message}");
            }
        }

        bool PurchaseOffline(ShopItemDto item)
        {
            // LocalShopService does the validation/spend/state mutation and
            // fires its OnItemPurchased; we forward that via OnLocalPurchased.
            bool ok = _local.PurchaseItem(item.ItemId);
            if (!ok) return false;

            EnqueueOfflinePurchase(item.ItemId, item.Price);
            return true;
        }

        void ApplyServerSuccess(ShopItemDto item, ServerShopService.ShopPurchaseResponse data)
        {
            // Server is authoritative for the fragment balance — overwrite the
            // local value rather than spending again. SetBalance lives on
            // LocalEconomyService (server-aware path), so cast through it.
            if (_economy is LocalEconomyService local)
                local.SetBalance(data.NewFragmentBalance);

            _local.ApplyServerPurchaseResult(item, data.ConsumablesUpdate);

            Debug.Log($"[HybridShopService] Server purchase applied: '{item.ItemId}' " +
                      $"(spent={data.FragmentsSpent}, balance={data.NewFragmentBalance}).");
        }

        void EnqueueOfflinePurchase(string itemId, int cachedPrice)
        {
            if (_syncQueue == null) return;

            var op = PendingOperation.Create(
                SyncOperationType.ShopPurchase,
                "POST " + ApiEndpoints.ShopPurchase,
                new Dictionary<string, object>
                {
                    ["itemId"] = itemId,
                    ["cachedPrice"] = cachedPrice
                });

            _syncQueue.Enqueue(op);
            Debug.Log($"[HybridShopService] Queued offline purchase '{itemId}' " +
                      $"@ cachedPrice={cachedPrice} (queue size={_syncQueue.Count}).");
        }

        void OnLocalPurchased(ShopItemDto item) => OnItemPurchased?.Invoke(item);

        #endregion

        #region Catalog refresh on reconnect

        void OnConnectivityChanged(bool isOnline)
        {
            if (!isOnline) return;
            _ = RefreshCatalogSafe();
        }

        async Task RefreshCatalogSafe()
        {
            try
            {
                if (_content == null) return;
                await _content.RefreshShopCatalogAsync();
                Debug.Log("[HybridShopService] Catalog refreshed after reconnect.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HybridShopService] Catalog refresh failed: {ex.Message}");
            }
        }

        #endregion

        ShopItemDto FindCatalogItem(string itemId)
        {
            var catalog = _content?.ShopCatalog;
            return catalog?.FirstOrDefault(i => i != null && i.ItemId == itemId);
        }
    }
}
