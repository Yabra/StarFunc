using System;
using System.Threading.Tasks;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Handles server reconciliation after local level validation (API.md §6.7).
    /// Flow: 1) Local check (instant feedback) → 2) POST /check/level → 3) Server result is authoritative on conflict.
    /// When offline: queues the check as a pending operation for later sync.
    /// </summary>
    public class ReconciliationHandler
    {
        readonly LevelCheckClient _levelCheckClient;
        readonly NetworkMonitor _networkMonitor;

        /// <summary>
        /// The newSaveVersion returned by the last successful server check.
        /// Consumers (e.g. HybridSaveService) should use this as expectedVersion for PUT /save.
        /// </summary>
        public int LastSaveVersion { get; private set; }

        /// <summary>
        /// Raised when the server result diverges from the local result.
        /// The payload is the authoritative server-derived <see cref="LevelResult"/>.
        /// </summary>
        public event Action<LevelResult> OnReconciliationConflict;

        /// <summary>
        /// Raised when the server returns a NO_LIVES error (422).
        /// The level attempt was not counted server-side.
        /// </summary>
        public event Action OnNoLivesRejected;

        public ReconciliationHandler(LevelCheckClient levelCheckClient, NetworkMonitor networkMonitor)
        {
            _levelCheckClient = levelCheckClient;
            _networkMonitor = networkMonitor;
        }

        /// <summary>
        /// Submit the level answer for server-side reconciliation.
        /// Compares the server result with the local result and fires
        /// <see cref="OnReconciliationConflict"/> when they diverge.
        /// </summary>
        /// <param name="levelId">Level identifier (e.g. "sector_2_level_05").</param>
        /// <param name="answer">The player's answer payload.</param>
        /// <param name="elapsedTime">Seconds since level start.</param>
        /// <param name="errorsBeforeSubmit">Number of errors accumulated before this submission.</param>
        /// <param name="attempt">Current attempt number (1-based).</param>
        /// <param name="localResult">The result computed locally for immediate feedback.</param>
        /// <returns>The authoritative <see cref="LevelResult"/>: server result when online, local otherwise.</returns>
        public async Task<LevelResult> Reconcile(
            string levelId,
            PlayerAnswer answer,
            float elapsedTime,
            int errorsBeforeSubmit,
            int attempt,
            LevelResult localResult)
        {
            if (!_networkMonitor.IsOnline)
            {
                EnqueueForLaterSync(levelId, answer, elapsedTime, errorsBeforeSubmit, attempt);
                return localResult;
            }

            ApiResult<LevelCheckResponse> apiResult;
            try
            {
                apiResult = await _levelCheckClient.CheckLevel(
                    levelId, answer, elapsedTime, errorsBeforeSubmit, attempt);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Reconciliation] Server check threw: {ex.Message}");
                EnqueueForLaterSync(levelId, answer, elapsedTime, errorsBeforeSubmit, attempt);
                return localResult;
            }

            // Network dropped during request.
            if (apiResult.WentOffline)
            {
                EnqueueForLaterSync(levelId, answer, elapsedTime, errorsBeforeSubmit, attempt);
                return localResult;
            }

            // 422 NO_LIVES — attempt not counted server-side.
            if (apiResult.HttpStatus == 422 && apiResult.Error?.Code == "NO_LIVES")
            {
                Debug.LogWarning("[Reconciliation] Server rejected: NO_LIVES.");
                OnNoLivesRejected?.Invoke();
                return localResult;
            }

            // Other server errors — trust local.
            if (!apiResult.IsSuccess)
            {
                Debug.LogWarning(
                    $"[Reconciliation] Server check failed ({apiResult.Error?.Code}), using local result.");
                return localResult;
            }

            var response = apiResult.Data;
            LastSaveVersion = response.NewSaveVersion;

            var serverResult = ToLevelResult(response);

            // Detect conflict between local and server result.
            if (HasConflict(localResult, serverResult))
            {
                Debug.Log(
                    $"[Reconciliation] Conflict detected — local: valid={localResult.IsValid}, " +
                    $"stars={localResult.Stars} | server: valid={serverResult.IsValid}, " +
                    $"stars={serverResult.Stars}. Server is authoritative.");
                OnReconciliationConflict?.Invoke(serverResult);
            }
            else
            {
                Debug.Log("[Reconciliation] Local and server results match.");
            }

            return serverResult;
        }

        /// <summary>
        /// Build a <see cref="LevelResult"/> from the server response.
        /// </summary>
        static LevelResult ToLevelResult(LevelCheckResponse response)
        {
            var r = response.Result;
            return new LevelResult
            {
                IsValid = r.IsValid,
                Stars = r.Stars,
                FragmentsEarned = r.FragmentsEarned,
                Time = r.Time,
                ErrorCount = r.ErrorCount,
                MatchPercentage = r.MatchPercentage,
                Errors = r.Errors,
                LevelFailed = response.LevelFailed,
                FailReason = response.FailReason
            };
        }

        /// <summary>
        /// Determines whether the local and server results conflict in a meaningful way.
        /// </summary>
        static bool HasConflict(LevelResult local, LevelResult server)
        {
            if (local.IsValid != server.IsValid) return true;
            if (local.Stars != server.Stars) return true;
            if (local.LevelFailed != server.LevelFailed) return true;
            if (local.FragmentsEarned != server.FragmentsEarned) return true;
            return false;
        }

        /// <summary>
        /// Queue the check_level operation for later synchronization (offline path).
        /// Currently logs a TODO; will integrate with SyncQueue when task 2.15 lands.
        /// </summary>
        void EnqueueForLaterSync(
            string levelId, PlayerAnswer answer, float elapsedTime,
            int errorsBeforeSubmit, int attempt)
        {
            Debug.Log(
                $"[Reconciliation] Offline — queuing check_level for '{levelId}' (attempt {attempt}).");

            // TODO: persist to SyncQueue when available (task 2.15).
            // Expected format:
            // {
            //   "type": "check_level",
            //   "endpoint": "POST /check/level",
            //   "payload": { levelId, answer, elapsedTime, errorsBeforeSubmit, attempt },
            //   "createdAt": <unix_timestamp>,
            //   "retries": 0
            // }
        }
    }
}
