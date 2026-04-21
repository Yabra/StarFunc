using System;
using System.Threading.Tasks;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Composite save service: saves locally (instant) and syncs with cloud when online.
    /// Implements ISaveService for drop-in replacement of LocalSaveService.
    /// </summary>
    public class HybridSaveService : ISaveService
    {
        readonly LocalSaveService _local;
        readonly CloudSaveClient _cloud;
        readonly SaveMerger _merger;
        readonly NetworkMonitor _networkMonitor;

        int _lastKnownServerVersion;

        public HybridSaveService(
            LocalSaveService local,
            CloudSaveClient cloud,
            SaveMerger merger,
            NetworkMonitor networkMonitor)
        {
            _local = local;
            _cloud = cloud;
            _merger = merger;
            _networkMonitor = networkMonitor;
        }

        /// <summary>
        /// Load local save and kick off a non-blocking cloud sync if online.
        /// Returns local data immediately; merged result is persisted asynchronously.
        /// </summary>
        public PlayerSaveData Load()
        {
            var local = _local.Load();

            if (_networkMonitor.IsOnline)
                _ = TrySyncFromCloudAsync(local);

            return local;
        }

        /// <summary>
        /// Save locally (instant), then queue cloud synchronization.
        /// </summary>
        public void Save(PlayerSaveData data)
        {
            _local.Save(data);

            if (_networkMonitor.IsOnline)
                _ = TrySaveToCloudAsync(data);
            // TODO: when SyncQueue (task 2.15) is available, enqueue instead of fire-and-forget
        }

        public void Delete()
        {
            _local.Delete();
        }

        public bool HasSave()
        {
            return _local.HasSave();
        }

        /// <summary>
        /// Boot-time helper: loads local, fetches cloud, merges, and returns the result.
        /// Call this once during initialization when you can afford to await.
        /// </summary>
        public async Task<PlayerSaveData> LoadWithCloudSyncAsync()
        {
            var local = _local.Load();

            if (!_networkMonitor.IsOnline)
                return local;

            try
            {
                var server = await _cloud.LoadFromCloud();

                if (server == null)
                    return local;

                _lastKnownServerVersion = server.Version;

                if (local == null)
                {
                    _local.Save(server);
                    return server;
                }

                var merged = _merger.Merge(local, server);
                _local.Save(merged);
                return merged;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"HybridSaveService: cloud sync failed during boot — {ex.Message}");
                return local;
            }
        }

        #region Async cloud operations

        async Task TrySyncFromCloudAsync(PlayerSaveData local)
        {
            try
            {
                var server = await _cloud.LoadFromCloud();

                if (server == null)
                    return;

                _lastKnownServerVersion = server.Version;

                if (local == null)
                    return;

                var merged = _merger.Merge(local, server);
                _local.Save(merged);
                Debug.Log("HybridSaveService: merged local + cloud save.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"HybridSaveService: background cloud sync failed — {ex.Message}");
            }
        }

        async Task TrySaveToCloudAsync(PlayerSaveData data)
        {
            try
            {
                var result = await _cloud.SaveToCloud(data, _lastKnownServerVersion);

                if (result.IsSuccess)
                {
                    _lastKnownServerVersion = result.ServerVersion;
                    return;
                }

                if (result.IsConflict && result.ServerSave != null)
                {
                    var merged = _merger.Merge(data, result.ServerSave);
                    _local.Save(merged);

                    // Retry with merged data against the server version that caused the conflict
                    var retry = await _cloud.SaveToCloud(merged, result.ServerVersion);
                    if (retry.IsSuccess)
                        _lastKnownServerVersion = retry.ServerVersion;

                    Debug.Log("HybridSaveService: resolved save conflict via merge.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"HybridSaveService: cloud save failed — {ex.Message}");
            }
        }

        #endregion
    }
}
