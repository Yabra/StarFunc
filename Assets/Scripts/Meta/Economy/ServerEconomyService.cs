using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    #region Economy response models

    [Serializable]
    public class BalanceResponse
    {
        [JsonProperty("totalFragments")] public int TotalFragments;
    }

    [Serializable]
    public class TransactionResponse
    {
        [JsonProperty("previousBalance")] public int PreviousBalance;
        [JsonProperty("newBalance")] public int NewBalance;
        [JsonProperty("transactionId")] public string TransactionId;
        [JsonProperty("progressUpdate")] public ProgressUpdateData ProgressUpdate;
        [JsonProperty("newSaveVersion")] public int NewSaveVersion;
    }

    [Serializable]
    public class ProgressUpdateData
    {
        [JsonProperty("levelProgress")] public Dictionary<string, LevelProgressData> LevelProgress;
        [JsonProperty("newFragmentBalance")] public int NewFragmentBalance;
        [JsonProperty("sectorStarsCollected")] public int SectorStarsCollected;
        [JsonProperty("unlockedLevels")] public List<string> UnlockedLevels;
        [JsonProperty("unlockedSectors")] public List<string> UnlockedSectors;
        [JsonProperty("sectorCompleted")] public bool SectorCompleted;
    }

    [Serializable]
    public class LevelProgressData
    {
        [JsonProperty("isCompleted")] public bool IsCompleted;
        [JsonProperty("bestStars")] public int BestStars;
        [JsonProperty("bestTime")] public float BestTime;
        [JsonProperty("attempts")] public int Attempts;
    }

    #endregion

    /// <summary>
    /// REST wrapper for economy endpoints (API.md §6.3).
    /// Idempotency-Key for POST is handled automatically by ApiClient.
    /// </summary>
    public class ServerEconomyService
    {
        readonly ApiClient _apiClient;

        public ServerEconomyService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// GET /economy/balance — fetch server-authoritative fragment balance.
        /// </summary>
        public async Task<ApiResult<BalanceResponse>> GetBalance()
        {
            return await _apiClient.Get<BalanceResponse>(ApiEndpoints.EconomyBalance);
        }

        /// <summary>
        /// POST /economy/transaction — atomic earn/spend.
        /// For reason "skip_level", response includes progressUpdate and newSaveVersion.
        /// </summary>
        /// <param name="type">"earn" or "spend"</param>
        /// <param name="amount">Positive amount of fragments.</param>
        /// <param name="reason">Transaction reason (e.g. "shop_purchase", "skip_level", "level_reward").</param>
        /// <param name="referenceId">Related entity ID (levelId, itemId, etc.).</param>
        public async Task<ApiResult<TransactionResponse>> PostTransaction(
            string type, int amount, string reason, string referenceId)
        {
            var body = new
            {
                type,
                amount,
                reason,
                referenceId
            };

            var result = await _apiClient.Post<TransactionResponse>(
                ApiEndpoints.EconomyTransaction, body);

            if (!result.IsSuccess)
                LogTransactionError(result);

            return result;
        }

        static void LogTransactionError(ApiResult<TransactionResponse> result)
        {
            if (result.Error == null) return;

            switch (result.Error.Code)
            {
                case "INSUFFICIENT_FUNDS":
                    Debug.LogWarning("[ServerEconomy] Insufficient funds for transaction.");
                    break;
                case "INVALID_TRANSACTION":
                    Debug.LogWarning($"[ServerEconomy] Invalid transaction: {result.Error.Message}");
                    break;
                default:
                    Debug.LogWarning(
                        $"[ServerEconomy] Transaction error {result.Error.Code}: {result.Error.Message}");
                    break;
            }
        }
    }
}
