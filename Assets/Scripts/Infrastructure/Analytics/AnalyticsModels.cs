using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Wire format for <c>POST /analytics/events</c> (API.md §5.7, §6.8).
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        [JsonProperty("eventName")] public string EventName;
        [JsonProperty("timestamp")] public long Timestamp;
        [JsonProperty("sessionId")] public string SessionId;
        [JsonProperty("params")] public Dictionary<string, object> Params;
    }

    [Serializable]
    public class AnalyticsBatchRequest
    {
        [JsonProperty("events")] public List<AnalyticsEvent> Events;
    }

    [Serializable]
    public class AnalyticsBatchResponse
    {
        [JsonProperty("accepted")] public int Accepted;
        [JsonProperty("rejected")] public int Rejected;
    }

    /// <summary>
    /// Persisted form of the analytics buffer
    /// (<c>persistentDataPath/analytics_queue.json</c>) — events that survived
    /// past app shutdown and need to be flushed on the next launch.
    /// </summary>
    [Serializable]
    public class AnalyticsQueueFile
    {
        [JsonProperty("events")] public List<AnalyticsEvent> Events = new();
    }

    /// <summary>Canonical event names — keep in sync with API.md §6.8 table.</summary>
    public static class AnalyticsEventNames
    {
        public const string SessionStart = "session_start";
        public const string SessionEnd = "session_end";
        public const string LevelStart = "level_start";
        public const string LevelComplete = "level_complete";
        public const string LevelFail = "level_fail";
        public const string LevelSkip = "level_skip";
        public const string SectorUnlock = "sector_unlock";
        public const string Purchase = "purchase";
        public const string HintUsed = "hint_used";
        public const string LifeLost = "life_lost";
        public const string LifeRestored = "life_restored";
        public const string ActionUndo = "action_undo";
        public const string LevelReset = "level_reset";
    }
}
