using System;
using System.Threading.Tasks;
using StarFunc.Core;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Composite lives service (API.md §10, §11).
    /// Online: delegates to ServerLivesService, updates local state from server response.
    /// Offline: falls back to LocalLivesService timer (local clock, small drift acceptable).
    /// Server time is the single source of truth for the restore timer.
    /// </summary>
    public class HybridLivesService : ILivesService
    {
        readonly LocalLivesService _local;
        readonly ServerLivesService _server;
        readonly NetworkMonitor _networkMonitor;
        readonly IEconomyService _economyService;

        public HybridLivesService(
            LocalLivesService local,
            ServerLivesService server,
            NetworkMonitor networkMonitor,
            IEconomyService economyService)
        {
            _local = local;
            _server = server;
            _networkMonitor = networkMonitor;
            _economyService = economyService;

            _networkMonitor.OnConnectivityChanged += OnConnectivityChanged;
        }

        #region ILivesService

        public int GetCurrentLives() => _local.GetCurrentLives();

        public int GetMaxLives() => _local.GetMaxLives();

        public bool HasLives() => _local.HasLives();

        public float GetTimeUntilNextRestore() => _local.GetTimeUntilNextRestore();

        public bool RestoreLife()
        {
            if (_networkMonitor.IsOnline)
            {
                _ = RestoreOneOnlineAsync();
                return true;
            }

            return _local.RestoreLife();
        }

        public bool RestoreAllLives()
        {
            if (_networkMonitor.IsOnline)
            {
                _ = RestoreAllOnlineAsync();
                return true;
            }

            return _local.RestoreAllLives();
        }

        #endregion

        #region Server sync

        /// <summary>
        /// Fetch server-authoritative lives state and overwrite local.
        /// </summary>
        public async Task SyncStateAsync()
        {
            if (!_networkMonitor.IsOnline) return;

            var result = await _server.GetLivesState();
            if (result.IsSuccess)
                ApplyServerState(result.Data);
        }

        #endregion

        #region Online restore flows

        async Task RestoreOneOnlineAsync()
        {
            try
            {
                var result = await _server.RestoreOne();

                if (result.IsSuccess)
                {
                    ApplyRestoreResponse(result.Data.CurrentLives, result.Data.LastLifeRestoreTimestamp);
                    ApplyServerFragments(result.Data.NewFragmentBalance);
                }
                else if (result.HttpStatus is 422 or 400)
                {
                    // Server rejected — re-sync to get authoritative state
                    await SyncStateAsync();
                }
                else if (result.WentOffline)
                {
                    // Went offline mid-request — fall back to local
                    _local.RestoreLife();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HybridLives] RestoreOne failed: {ex.Message}");
                _local.RestoreLife();
            }
        }

        async Task RestoreAllOnlineAsync()
        {
            try
            {
                var result = await _server.RestoreAll();

                if (result.IsSuccess)
                {
                    ApplyRestoreResponse(result.Data.CurrentLives, result.Data.LastLifeRestoreTimestamp);
                    ApplyServerFragments(result.Data.NewFragmentBalance);
                }
                else if (result.HttpStatus is 422 or 400)
                {
                    await SyncStateAsync();
                }
                else if (result.WentOffline)
                {
                    _local.RestoreAllLives();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HybridLives] RestoreAll failed: {ex.Message}");
                _local.RestoreAllLives();
            }
        }

        #endregion

        #region Internal

        void ApplyServerState(LivesStateResponse state)
        {
            _local.SetState(
                state.CurrentLives,
                state.SecondsUntilNextRestore,
                state.LastLifeRestoreTimestamp);
        }

        void ApplyRestoreResponse(int currentLives, long lastLifeRestoreTimestamp)
        {
            float secondsUntilRestore = currentLives >= _local.GetMaxLives() ? 0f : _local.GetTimeUntilNextRestore();
            _local.SetState(currentLives, secondsUntilRestore, lastLifeRestoreTimestamp);
        }

        void ApplyServerFragments(int newBalance)
        {
            if (_economyService is HybridEconomyService hybrid)
                _ = hybrid.SyncBalanceAsync();
        }

        void OnConnectivityChanged(bool isOnline)
        {
            if (isOnline)
                _ = SyncStateAsync();
        }

        #endregion
    }
}
