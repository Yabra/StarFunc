using System.Collections.Generic;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Lightweight analytics contract (API.md §6.8). Events are buffered to
    /// disk and flushed in batches by <see cref="AnalyticsService"/>; callers
    /// only need to fire-and-forget via <see cref="TrackEvent"/>.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>Track a named event with arbitrary string-keyed parameters.</summary>
        void TrackEvent(string eventName, Dictionary<string, object> parameters = null);

        /// <summary>Active session id (uuid). Same value across all events of one app launch.</summary>
        string SessionId { get; }
    }
}
