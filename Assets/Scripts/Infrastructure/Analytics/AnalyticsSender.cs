using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Pure-class REST wrapper for <c>POST /analytics/events</c> (API.md §6.8,
    /// task 4.8a). Returns whether the batch was accepted server-side; the
    /// caller (<see cref="AnalyticsService"/>) decides whether to drop or
    /// retain events based on that signal.
    /// </summary>
    public class AnalyticsSender
    {
        readonly ApiClient _apiClient;
        readonly NetworkMonitor _networkMonitor;

        public AnalyticsSender(ApiClient apiClient, NetworkMonitor networkMonitor)
        {
            _apiClient = apiClient;
            _networkMonitor = networkMonitor;
        }

        /// <summary>
        /// POST a batch. Returns true on 2xx (server accepted the events,
        /// caller can drop them locally). Returns false on offline / network
        /// error (caller should keep events buffered for the next attempt).
        /// </summary>
        public async Task<bool> SendAsync(IReadOnlyList<AnalyticsEvent> events)
        {
            if (events == null || events.Count == 0) return true;
            if (!_networkMonitor.IsOnline) return false;

            var body = new AnalyticsBatchRequest { Events = new List<AnalyticsEvent>(events) };
            var result = await _apiClient.Post<AnalyticsBatchResponse>(
                ApiEndpoints.AnalyticsEvents, body);

            if (result.IsSuccess)
            {
                int accepted = result.Data?.Accepted ?? events.Count;
                int rejected = result.Data?.Rejected ?? 0;
                Debug.Log($"[AnalyticsSender] {accepted} accepted, {rejected} rejected " +
                          $"(batch size {events.Count}).");
                return true;
            }

            if (result.WentOffline)
            {
                Debug.Log("[AnalyticsSender] Lost connectivity mid-batch; keeping events buffered.");
                return false;
            }

            Debug.LogWarning($"[AnalyticsSender] Batch send failed — " +
                             $"{result.Error?.Code}: {result.Error?.Message}. Will retry.");
            return false;
        }
    }
}
