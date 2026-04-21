using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Processes the SyncQueue when connectivity is restored (API.md §7.4, Appendix A.4).
    /// Algorithm: 1) POST /auth/refresh → 2) Process sync queue FIFO → 3) PUT /save → 4) POST /analytics/events.
    /// </summary>
    public class SyncProcessor
    {
        readonly SyncQueue _syncQueue;
        readonly ApiClient _apiClient;
        readonly AuthService _authService;
        readonly CloudSaveClient _cloudSaveClient;
        readonly NetworkMonitor _networkMonitor;
        readonly ISaveService _saveService;

        bool _isProcessing;

        public bool IsProcessing => _isProcessing;

        public SyncProcessor(
            SyncQueue syncQueue,
            ApiClient apiClient,
            AuthService authService,
            CloudSaveClient cloudSaveClient,
            NetworkMonitor networkMonitor,
            ISaveService saveService)
        {
            _syncQueue = syncQueue;
            _apiClient = apiClient;
            _authService = authService;
            _cloudSaveClient = cloudSaveClient;
            _networkMonitor = networkMonitor;
            _saveService = saveService;

            _networkMonitor.OnConnectivityChanged += OnConnectivityChanged;
        }

        public void Dispose()
        {
            _networkMonitor.OnConnectivityChanged -= OnConnectivityChanged;
        }

        void OnConnectivityChanged(bool isOnline)
        {
            if (isOnline && _syncQueue.Count > 0)
                _ = ProcessAsync();
        }

        /// <summary>
        /// Process the entire sync queue. Safe to call multiple times — re-entrant guard prevents overlap.
        /// </summary>
        public async Task ProcessAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                Debug.Log($"[SyncProcessor] Starting sync — {_syncQueue.Count} operations queued.");

                // Step 1: Refresh auth token
                bool refreshed = await _authService.InitializeAsync();
                if (!refreshed && _networkMonitor.IsOnline)
                {
                    Debug.LogWarning("[SyncProcessor] Auth refresh failed, but still online — proceeding.");
                }

                if (!_networkMonitor.IsOnline)
                {
                    Debug.LogWarning("[SyncProcessor] Lost connectivity during auth refresh — aborting.");
                    return;
                }

                // Step 2: Process sync queue FIFO
                int processed = 0;
                int failed = 0;

                while (_syncQueue.Count > 0)
                {
                    if (!_networkMonitor.IsOnline)
                    {
                        Debug.LogWarning(
                            $"[SyncProcessor] Lost connectivity mid-queue — {_syncQueue.Count} operations remaining.");
                        return;
                    }

                    var operation = _syncQueue.Peek();
                    bool success = await ProcessOperation(operation);

                    // Always dequeue — each operation is independent, failures don't block the queue
                    _syncQueue.Dequeue();

                    if (success)
                        processed++;
                    else
                        failed++;
                }

                Debug.Log($"[SyncProcessor] Queue processed — {processed} succeeded, {failed} failed.");

                // Step 3: PUT /save with final state
                await SaveFinalState();

                // Step 4: POST /analytics/events (fire-and-forget)
                await FlushAnalytics();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SyncProcessor] Unexpected error: {ex}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        async Task<bool> ProcessOperation(PendingOperation operation)
        {
            try
            {
                Debug.Log($"[SyncProcessor] Processing {operation.Type} (id={operation.Id})");

                // Parse "POST /check/level" → method="POST", path="/check/level"
                var (method, path) = ParseEndpoint(operation.Endpoint);
                object payload = NormalizePayload(operation.Payload);

                ApiResult<JObject> result;

                switch (method)
                {
                    case "POST":
                        result = await _apiClient.Post<JObject>(path, payload);
                        break;
                    case "PUT":
                        result = await _apiClient.Put<JObject>(path, payload);
                        break;
                    case "GET":
                        result = await _apiClient.Get<JObject>(path);
                        break;
                    default:
                        Debug.LogError(
                            $"[SyncProcessor] Unsupported method '{method}' for operation {operation.Id}");
                        return false;
                }

                if (result.WentOffline)
                    return false;

                if (result.IsSuccess)
                {
                    Debug.Log($"[SyncProcessor] {operation.Type} (id={operation.Id}) succeeded.");
                    return true;
                }

                // Handle check_level rejection: accept server decision, continue queue
                if (operation.Type == SyncOperationType.CheckLevel)
                {
                    Debug.LogWarning(
                        $"[SyncProcessor] check_level (id={operation.Id}) rejected by server — " +
                        $"{result.Error?.Code}: {result.Error?.Message}. Accepting server decision.");
                    return false;
                }

                Debug.LogWarning(
                    $"[SyncProcessor] {operation.Type} (id={operation.Id}) failed — " +
                    $"HTTP {result.HttpStatus}, {result.Error?.Code}: {result.Error?.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SyncProcessor] Exception processing {operation.Type} (id={operation.Id}): {ex.Message}");
                return false;
            }
        }

        async Task SaveFinalState()
        {
            if (!_networkMonitor.IsOnline) return;

            try
            {
                var save = _saveService.Load();
                if (save == null) return;

                var saveResult = await _cloudSaveClient.SaveToCloud(save, save.Version);

                if (saveResult.IsSuccess)
                {
                    Debug.Log("[SyncProcessor] Final save uploaded successfully.");
                }
                else if (saveResult.IsConflict)
                {
                    Debug.LogWarning("[SyncProcessor] Final save conflict — will be resolved on next full sync.");
                }
                else
                {
                    Debug.LogWarning("[SyncProcessor] Final save upload failed.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SyncProcessor] SaveFinalState error: {ex.Message}");
            }
        }

        async Task FlushAnalytics()
        {
            if (!_networkMonitor.IsOnline) return;

            try
            {
                // POST /analytics/events with empty batch — server accepts 202
                // The analytics buffer is managed externally; this is a signal to flush.
                await _apiClient.Post<object>(ApiEndpoints.AnalyticsEvents, new { events = Array.Empty<object>() });
                Debug.Log("[SyncProcessor] Analytics flush sent.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SyncProcessor] Analytics flush failed: {ex.Message}");
            }
        }

        #region Helpers

        static (string method, string path) ParseEndpoint(string endpoint)
        {
            // Format: "POST /check/level" or "PUT /save"
            if (string.IsNullOrEmpty(endpoint))
                return ("POST", "/");

            int spaceIdx = endpoint.IndexOf(' ');
            if (spaceIdx < 0)
                return ("POST", endpoint);

            string method = endpoint.Substring(0, spaceIdx).ToUpperInvariant();
            string path = endpoint.Substring(spaceIdx + 1);
            return (method, path);
        }

        /// <summary>
        /// Payload may be deserialized as JObject from disk — pass through as-is for serialization.
        /// </summary>
        static object NormalizePayload(object payload)
        {
            return payload;
        }

        #endregion
    }
}
