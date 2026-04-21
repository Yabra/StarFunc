using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Persistent FIFO queue of offline mutations (API.md §7.4).
    /// Stored at Application.persistentDataPath/sync_queue.json and survives app restarts.
    /// </summary>
    public class SyncQueue
    {
        const string FileName = "sync_queue.json";

        readonly string _filePath;
        readonly List<PendingOperation> _operations;
        readonly object _lock = new();

        public int Count
        {
            get { lock (_lock) return _operations.Count; }
        }

        public SyncQueue()
        {
            _filePath = Path.Combine(Application.persistentDataPath, FileName);
            _operations = LoadFromDisk();
        }

        public void Enqueue(PendingOperation operation)
        {
            lock (_lock)
            {
                _operations.Add(operation);
                SaveToDisk();
            }
        }

        public PendingOperation Dequeue()
        {
            lock (_lock)
            {
                if (_operations.Count == 0)
                    return null;

                var op = _operations[0];
                _operations.RemoveAt(0);
                SaveToDisk();
                return op;
            }
        }

        public PendingOperation Peek()
        {
            lock (_lock)
            {
                return _operations.Count > 0 ? _operations[0] : null;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _operations.Clear();
                SaveToDisk();
            }
        }

        #region Persistence

        void SaveToDisk()
        {
            try
            {
                var wrapper = new SyncQueueFile { PendingOperations = _operations };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SyncQueue] Failed to save queue: {ex.Message}");
            }
        }

        List<PendingOperation> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<PendingOperation>();

                string json = File.ReadAllText(_filePath);
                var wrapper = JsonConvert.DeserializeObject<SyncQueueFile>(json);
                return wrapper?.PendingOperations ?? new List<PendingOperation>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SyncQueue] Failed to load queue: {ex.Message}");
                return new List<PendingOperation>();
            }
        }

        #endregion
    }

    #region Models

    [Serializable]
    public class SyncQueueFile
    {
        [JsonProperty("pendingOperations")]
        public List<PendingOperation> PendingOperations = new();
    }

    [Serializable]
    public class PendingOperation
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("type")] public string Type;
        [JsonProperty("endpoint")] public string Endpoint;
        [JsonProperty("payload")] public object Payload;
        [JsonProperty("createdAt")] public long CreatedAt;
        [JsonProperty("retries")] public int Retries;

        /// <summary>
        /// Creates a new PendingOperation with a fresh UUID and current timestamp.
        /// </summary>
        public static PendingOperation Create(string type, string endpoint, object payload)
        {
            return new PendingOperation
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Endpoint = endpoint,
                Payload = payload,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Retries = 0
            };
        }
    }

    /// <summary>
    /// Known operation types for PendingOperation.Type (API.md §7.4).
    /// </summary>
    public static class SyncOperationType
    {
        public const string CheckLevel = "check_level";
        public const string ShopPurchase = "shop_purchase";
        public const string EconomyTransaction = "economy_transaction";
    }

    #endregion
}
