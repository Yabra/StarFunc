using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    public class CloudSaveClient
    {
        readonly ApiClient _apiClient;

        public CloudSaveClient(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// GET /save — load cloud save. Returns null if no cloud save exists.
        /// </summary>
        public async Task<PlayerSaveData> LoadFromCloud()
        {
            var result = await _apiClient.Get<CloudSaveResponse>(ApiEndpoints.Save);

            if (!result.IsSuccess)
            {
                Debug.LogWarning(
                    $"CloudSaveClient: LoadFromCloud failed — {result.Error?.Code}: {result.Error?.Message}");
                return null;
            }

            if (result.Data == null || !result.Data.Exists)
                return null;

            return result.Data.Save;
        }

        /// <summary>
        /// PUT /save — save to cloud with optimistic lock.
        /// On 409 SAVE_CONFLICT returns the server save from details.serverSave for merging.
        /// </summary>
        public async Task<CloudSaveResult> SaveToCloud(PlayerSaveData data, int expectedVersion)
        {
            var body = new SaveRequest { Save = data, ExpectedVersion = expectedVersion };
            var result = await _apiClient.Put<SaveAcceptedResponse>(ApiEndpoints.Save, body);

            if (result.IsSuccess)
            {
                return new CloudSaveResult
                {
                    IsSuccess = true,
                    ServerVersion = result.Data?.Version ?? 0
                };
            }

            if (result.HttpStatus == 409 && result.Error?.Code == "SAVE_CONFLICT")
            {
                var details = ParseConflictDetails(result.Error.Details);
                return new CloudSaveResult
                {
                    IsConflict = true,
                    ServerSave = details?.ServerSave,
                    ServerVersion = details?.ServerVersion ?? 0
                };
            }

            return new CloudSaveResult { WentOffline = result.WentOffline };
        }

        static SaveConflictDetails ParseConflictDetails(object details)
        {
            if (details == null) return null;

            try
            {
                if (details is JObject jObj)
                    return jObj.ToObject<SaveConflictDetails>();

                var json = JsonConvert.SerializeObject(details);
                return JsonConvert.DeserializeObject<SaveConflictDetails>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudSaveClient: failed to parse conflict details — {ex.Message}");
                return null;
            }
        }

        #region DTOs

        [Serializable]
        class CloudSaveResponse
        {
            [JsonProperty("save")] public PlayerSaveData Save;
            [JsonProperty("exists")] public bool Exists;
        }

        [Serializable]
        class SaveRequest
        {
            [JsonProperty("save")] public PlayerSaveData Save;
            [JsonProperty("expectedVersion")] public int ExpectedVersion;
        }

        [Serializable]
        class SaveAcceptedResponse
        {
            [JsonProperty("version")] public int Version;
            [JsonProperty("accepted")] public bool Accepted;
        }

        [Serializable]
        class SaveConflictDetails
        {
            [JsonProperty("serverVersion")] public int ServerVersion;
            [JsonProperty("serverSave")] public PlayerSaveData ServerSave;
        }

        #endregion
    }

    public class CloudSaveResult
    {
        public bool IsSuccess;
        public bool IsConflict;
        public PlayerSaveData ServerSave;
        public int ServerVersion;
        public bool WentOffline;
    }
}
