using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    #region Lives response models

    [Serializable]
    public class LivesStateResponse
    {
        [JsonProperty("currentLives")] public int CurrentLives;
        [JsonProperty("maxLives")] public int MaxLives;
        [JsonProperty("secondsUntilNextRestore")] public float SecondsUntilNextRestore;
        [JsonProperty("restoreIntervalSeconds")] public int RestoreIntervalSeconds;
        [JsonProperty("lastLifeRestoreTimestamp")] public long LastLifeRestoreTimestamp;
    }

    [Serializable]
    public class RestoreLifeResponse
    {
        [JsonProperty("currentLives")] public int CurrentLives;
        [JsonProperty("fragmentsSpent")] public int FragmentsSpent;
        [JsonProperty("newFragmentBalance")] public int NewFragmentBalance;
        [JsonProperty("lastLifeRestoreTimestamp")] public long LastLifeRestoreTimestamp;
    }

    [Serializable]
    public class RestoreAllLivesResponse
    {
        [JsonProperty("currentLives")] public int CurrentLives;
        [JsonProperty("fragmentsSpent")] public int FragmentsSpent;
        [JsonProperty("newFragmentBalance")] public int NewFragmentBalance;
        [JsonProperty("livesRestored")] public int LivesRestored;
        [JsonProperty("lastLifeRestoreTimestamp")] public long LastLifeRestoreTimestamp;
    }

    #endregion

    /// <summary>
    /// REST wrapper for lives endpoints (API.md §6.4).
    /// Server recalculates lives from lastLifeRestoreTimestamp on every request.
    /// </summary>
    public class ServerLivesService
    {
        readonly ApiClient _apiClient;

        public ServerLivesService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// GET /lives — server-authoritative lives state.
        /// Server recalculates based on lastLifeRestoreTimestamp + serverTime.
        /// </summary>
        public async Task<ApiResult<LivesStateResponse>> GetLivesState()
        {
            return await _apiClient.Get<LivesStateResponse>(ApiEndpoints.Lives);
        }

        /// <summary>
        /// POST /lives/restore — restore one life for fragments.
        /// </summary>
        public async Task<ApiResult<RestoreLifeResponse>> RestoreOne()
        {
            var body = new { paymentMethod = "fragments" };

            var result = await _apiClient.Post<RestoreLifeResponse>(
                ApiEndpoints.LivesRestore, body);

            if (!result.IsSuccess)
                LogRestoreError(result.Error, "RestoreOne");

            return result;
        }

        /// <summary>
        /// POST /lives/restore-all — restore all lives for fragments.
        /// Cost = restoreCostFragments × (maxLives - currentLives), calculated server-side.
        /// </summary>
        public async Task<ApiResult<RestoreAllLivesResponse>> RestoreAll()
        {
            var body = new { paymentMethod = "fragments" };

            var result = await _apiClient.Post<RestoreAllLivesResponse>(
                ApiEndpoints.LivesRestoreAll, body);

            if (!result.IsSuccess)
                LogRestoreError(result.Error, "RestoreAll");

            return result;
        }

        static void LogRestoreError(ApiError error, string operation)
        {
            if (error == null) return;

            switch (error.Code)
            {
                case "INSUFFICIENT_FUNDS":
                    Debug.LogWarning($"[ServerLives] {operation}: insufficient fragments.");
                    break;
                case "INVALID_REQUEST":
                    Debug.LogWarning($"[ServerLives] {operation}: lives already full.");
                    break;
                default:
                    Debug.LogWarning(
                        $"[ServerLives] {operation} error {error.Code}: {error.Message}");
                    break;
            }
        }
    }
}
