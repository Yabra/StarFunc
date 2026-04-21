using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// REST client for POST /check/level (API.md §6.7).
    /// Sends the player's answer for server-side reconciliation.
    /// </summary>
    public class LevelCheckClient
    {
        readonly ApiClient _apiClient;

        public LevelCheckClient(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Submit a level answer to the server for authoritative validation.
        /// Returns <see cref="LevelCheckResult"/> on success, or error details on failure.
        /// </summary>
        public async Task<ApiResult<LevelCheckResponse>> CheckLevel(
            string levelId,
            PlayerAnswer answer,
            float elapsedTime,
            int errorsBeforeSubmit,
            int attempt)
        {
            var body = new LevelCheckRequest
            {
                LevelId = levelId,
                Answer = answer,
                ElapsedTime = elapsedTime,
                ErrorsBeforeSubmit = errorsBeforeSubmit,
                Attempt = attempt
            };

            var result = await _apiClient.Post<LevelCheckResponse>(ApiEndpoints.CheckLevel, body);

            if (!result.IsSuccess)
            {
                // 422 NO_LIVES — player has 0 lives, attempt not counted.
                if (result.HttpStatus == 422 && result.Error?.Code == "NO_LIVES")
                {
                    Debug.LogWarning("[LevelCheckClient] Server rejected: NO_LIVES (0 lives remaining).");
                }
                else
                {
                    Debug.LogWarning(
                        $"[LevelCheckClient] CheckLevel failed — " +
                        $"HTTP {result.HttpStatus}, {result.Error?.Code}: {result.Error?.Message}");
                }
            }

            return result;
        }
    }

    #region Request / Response DTOs

    [Serializable]
    public class LevelCheckRequest
    {
        [JsonProperty("levelId")] public string LevelId;
        [JsonProperty("answer")] public PlayerAnswer Answer;
        [JsonProperty("elapsedTime")] public float ElapsedTime;
        [JsonProperty("errorsBeforeSubmit")] public int ErrorsBeforeSubmit;
        [JsonProperty("attempt")] public int Attempt;
    }

    [Serializable]
    public class LevelCheckResponse
    {
        [JsonProperty("result")] public LevelCheckResult Result;
        [JsonProperty("progressUpdate")] public ProgressUpdate ProgressUpdate;
        [JsonProperty("livesUpdate")] public LivesUpdate LivesUpdate;
        [JsonProperty("levelFailed")] public bool LevelFailed;
        [JsonProperty("failReason")] public string FailReason;
        [JsonProperty("newSaveVersion")] public int NewSaveVersion;
    }

    [Serializable]
    public class LevelCheckResult
    {
        [JsonProperty("isValid")] public bool IsValid;
        [JsonProperty("stars")] public int Stars;
        [JsonProperty("fragmentsEarned")] public int FragmentsEarned;
        [JsonProperty("time")] public float Time;
        [JsonProperty("errorCount")] public int ErrorCount;
        [JsonProperty("matchPercentage")] public float MatchPercentage;
        [JsonProperty("errors")] public string[] Errors;
    }

    [Serializable]
    public class ProgressUpdate
    {
        [JsonProperty("levelProgress")] public Dictionary<string, LevelProgressEntry> LevelProgress;
        [JsonProperty("newFragmentBalance")] public int NewFragmentBalance;
        [JsonProperty("sectorStarsCollected")] public int SectorStarsCollected;
        [JsonProperty("unlockedLevels")] public string[] UnlockedLevels;
        [JsonProperty("unlockedSectors")] public string[] UnlockedSectors;
        [JsonProperty("sectorCompleted")] public bool SectorCompleted;
    }

    [Serializable]
    public class LevelProgressEntry
    {
        [JsonProperty("isCompleted")] public bool IsCompleted;
        [JsonProperty("bestStars")] public int BestStars;
        [JsonProperty("bestTime")] public float BestTime;
        [JsonProperty("attempts")] public int Attempts;
    }

    [Serializable]
    public class LivesUpdate
    {
        [JsonProperty("currentLives")] public int CurrentLives;
        [JsonProperty("secondsUntilNextRestore")] public float SecondsUntilNextRestore;
        [JsonProperty("lastLifeRestoreTimestamp")] public long LastLifeRestoreTimestamp;
    }

    #endregion
}
