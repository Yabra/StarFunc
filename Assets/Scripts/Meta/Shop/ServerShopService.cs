using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    /// <summary>
    /// REST wrapper for the server shop endpoints (API.md §6.5):
    /// <c>GET /shop/items</c> and <c>POST /shop/purchase</c>. Pure plumbing —
    /// no local state, no economy/save mutations. <see cref="HybridShopService"/>
    /// composes this with <see cref="LocalShopService"/> and applies the
    /// results.
    /// </summary>
    public class ServerShopService
    {
        readonly ApiClient _api;

        public ServerShopService(ApiClient api)
        {
            _api = api;
        }

        public async Task<ApiResult<ShopCatalogResponse>> FetchCatalog()
        {
            return await _api.Get<ShopCatalogResponse>(ApiEndpoints.ShopItems);
        }

        public async Task<ApiResult<ShopPurchaseResponse>> Purchase(string itemId, int cachedPrice)
        {
            var body = new ShopPurchaseRequest { ItemId = itemId, CachedPrice = cachedPrice };
            return await _api.Post<ShopPurchaseResponse>(ApiEndpoints.ShopPurchase, body);
        }

        #region Wire format

        [Serializable]
        public class ShopCatalogResponse
        {
            [JsonProperty("items")] public ShopItemDto[] Items;
            [JsonProperty("catalogVersion")] public int CatalogVersion;
        }

        [Serializable]
        public class ShopPurchaseRequest
        {
            [JsonProperty("itemId")] public string ItemId;
            [JsonProperty("cachedPrice")] public int CachedPrice;
        }

        [Serializable]
        public class ShopPurchaseResponse
        {
            [JsonProperty("purchased")] public bool Purchased;
            [JsonProperty("itemId")] public string ItemId;
            [JsonProperty("fragmentsSpent")] public int FragmentsSpent;
            [JsonProperty("newFragmentBalance")] public int NewFragmentBalance;
            [JsonProperty("consumablesUpdate")] public Dictionary<string, int> ConsumablesUpdate;
        }

        #endregion
    }
}
