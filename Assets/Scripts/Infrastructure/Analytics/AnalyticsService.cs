using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Disk-buffered analytics queue (API.md §6.8, task 4.8/4.8a).
    /// <para>
    /// • <see cref="TrackEvent"/> appends an event to an in-memory list and
    ///   atomically rewrites <c>analytics_queue.json</c> in
    ///   <see cref="Application.persistentDataPath"/> so unsent events survive
    ///   crashes/restarts.<br/>
    /// • Periodic flush every <see cref="FlushIntervalSeconds"/>s and on
    ///   <see cref="OnApplicationPause(bool)"/> sends the buffer through
    ///   <see cref="AnalyticsSender"/>; on success the buffer is cleared.<br/>
    /// • Driven by a hidden DontDestroyOnLoad MonoBehaviour
    ///   (<see cref="AnalyticsHost"/>) created in <see cref="Initialize"/>.
    /// </para>
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        const string QueueFileName = "analytics_queue.json";
        const float FlushIntervalSeconds = 30f;

        readonly AnalyticsSender _sender;
        readonly NetworkMonitor _networkMonitor;
        readonly string _queueFilePath;
        readonly object _lock = new();
        readonly List<AnalyticsEvent> _events;

        bool _flushing;
        bool _initialized;
        AnalyticsHost _host;

        public string SessionId { get; }

        public AnalyticsService(AnalyticsSender sender, NetworkMonitor networkMonitor)
        {
            _sender = sender;
            _networkMonitor = networkMonitor;
            _queueFilePath = Path.Combine(Application.persistentDataPath, QueueFileName);
            _events = LoadFromDisk();
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Spin up the host MonoBehaviour and emit <c>session_start</c>.
        /// Call once during boot.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var go = new GameObject("[AnalyticsService]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<AnalyticsHost>();
            _host.Bind(this, FlushIntervalSeconds);

            TrackEvent(AnalyticsEventNames.SessionStart);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            var evt = new AnalyticsEvent
            {
                EventName = eventName,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SessionId = SessionId,
                Params = parameters ?? new Dictionary<string, object>()
            };

            lock (_lock)
            {
                _events.Add(evt);
                SaveToDisk();
            }

            Debug.Log($"[AnalyticsService] track '{eventName}' " +
                      $"(buffered={_events.Count}, params={parameters?.Count ?? 0}).");
        }

        /// <summary>
        /// Send everything buffered. Called by the host on the periodic timer
        /// and on app pause; also surfaced for the SyncProcessor offline-flush
        /// step (API.md App. A.4 step 4).
        /// </summary>
        public async Task FlushAsync()
        {
            if (_flushing) return;
            _flushing = true;

            try
            {
                if (!_networkMonitor.IsOnline) return;
                if (_sender == null) return;

                AnalyticsEvent[] snapshot;
                lock (_lock)
                {
                    if (_events.Count == 0) return;
                    snapshot = _events.ToArray();
                }

                bool ok = await _sender.SendAsync(snapshot);
                if (!ok) return;

                // Server accepted; remove the events we sent. Anything appended
                // during the in-flight send remains for the next flush.
                lock (_lock)
                {
                    int sent = snapshot.Length;
                    if (sent >= _events.Count) _events.Clear();
                    else _events.RemoveRange(0, sent);
                    SaveToDisk();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsService] FlushAsync error: {ex}");
            }
            finally
            {
                _flushing = false;
            }
        }

        public void HandleApplicationPause(bool paused)
        {
            if (paused)
            {
                TrackEvent(AnalyticsEventNames.SessionEnd, new Dictionary<string, object>
                {
                    ["duration"] = (long)Time.realtimeSinceStartup
                });
            }

            // Fire-and-forget; OnApplicationPause runs synchronously and
            // returning a Task here would block the engine.
            _ = FlushAsync();
        }

        #region Persistence

        void SaveToDisk()
        {
            try
            {
                var wrapper = new AnalyticsQueueFile { Events = _events };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                File.WriteAllText(_queueFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsService] Save failed: {ex.Message}");
            }
        }

        List<AnalyticsEvent> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_queueFilePath)) return new List<AnalyticsEvent>();
                string json = File.ReadAllText(_queueFilePath);
                var wrapper = JsonConvert.DeserializeObject<AnalyticsQueueFile>(json);
                return wrapper?.Events ?? new List<AnalyticsEvent>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsService] Load failed: {ex.Message}");
                return new List<AnalyticsEvent>();
            }
        }

        #endregion

        /// <summary>
        /// Hidden MonoBehaviour pumping the periodic flush + forwarding pause
        /// events. Owns no state — just drives the service.
        /// </summary>
        class AnalyticsHost : MonoBehaviour
        {
            AnalyticsService _service;
            float _interval;
            float _timer;

            public void Bind(AnalyticsService service, float interval)
            {
                _service = service;
                _interval = interval;
                _timer = 0f;
            }

            void Update()
            {
                if (_service == null) return;
                _timer += Time.unscaledDeltaTime;
                if (_timer < _interval) return;
                _timer = 0f;
                _ = _service.FlushAsync();
            }

            void OnApplicationPause(bool pause)
            {
                _service?.HandleApplicationPause(pause);
            }

            void OnApplicationQuit()
            {
                _service?.HandleApplicationPause(true);
            }
        }
    }
}
