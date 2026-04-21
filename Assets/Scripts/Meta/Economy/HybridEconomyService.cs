using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Composite economy service (API.md §10.2).
    /// Online: proxies through ServerEconomyService, updates local balance from server response.
    /// Offline: executes through LocalEconomyService, queues transaction for later sync.
    /// Server balance is the single source of truth.
    /// </summary>
    public class HybridEconomyService : IEconomyService
    {
        readonly LocalEconomyService _local;
        readonly ServerEconomyService _server;
        readonly NetworkMonitor _networkMonitor;

        readonly List<PendingTransaction> _pendingTransactions = new();
        bool _isFlushing;

        public HybridEconomyService(
            LocalEconomyService local,
            ServerEconomyService server,
            NetworkMonitor networkMonitor)
        {
            _local = local;
            _server = server;
            _networkMonitor = networkMonitor;

            _networkMonitor.OnConnectivityChanged += OnConnectivityChanged;
        }

        #region IEconomyService

        public int GetFragments() => _local.GetFragments();

        public void AddFragments(int amount)
        {
            if (amount <= 0) return;

            _local.AddFragments(amount);

            if (_networkMonitor.IsOnline)
                _ = PostTransactionAsync("earn", amount, "level_reward", null);
            else
                Enqueue("earn", amount, "level_reward", null);
        }

        public bool SpendFragments(int amount)
        {
            if (amount <= 0) return false;
            if (!CanAfford(amount)) return false;

            bool spent = _local.SpendFragments(amount);
            if (!spent) return false;

            if (_networkMonitor.IsOnline)
                _ = PostTransactionAsync("spend", amount, "shop_purchase", null);
            else
                Enqueue("spend", amount, "shop_purchase", null);

            return true;
        }

        public bool CanAfford(int amount) => _local.CanAfford(amount);

        #endregion

        #region Server-aware methods

        /// <summary>
        /// Post a transaction with explicit reason and referenceId.
        /// Use for operations that need the full server response (e.g. skip_level with progressUpdate).
        /// Falls back to local execution + queue when offline.
        /// </summary>
        public async Task<ApiResult<TransactionResponse>> PostTransaction(
            string type, int amount, string reason, string referenceId)
        {
            if (_networkMonitor.IsOnline)
            {
                var result = await _server.PostTransaction(type, amount, reason, referenceId);

                if (result.IsSuccess)
                {
                    ApplyServerBalance(result.Data.NewBalance);
                }
                else if (result.HttpStatus is 422 or 400)
                {
                    // Server rejected — re-sync balance to get authoritative state
                    await SyncBalanceAsync();
                }

                return result;
            }

            // Offline: execute locally and queue for later
            if (type == "spend")
                _local.SpendFragments(amount);
            else
                _local.AddFragments(amount);

            Enqueue(type, amount, reason, referenceId);

            return new ApiResult<TransactionResponse> { WentOffline = true };
        }

        /// <summary>
        /// Fetch the server-authoritative balance and overwrite local.
        /// </summary>
        public async Task SyncBalanceAsync()
        {
            if (!_networkMonitor.IsOnline) return;

            var result = await _server.GetBalance();
            if (result.IsSuccess)
                ApplyServerBalance(result.Data.TotalFragments);
        }

        /// <summary>
        /// Send all queued offline transactions to the server.
        /// Called automatically on reconnect.
        /// </summary>
        public async Task FlushPendingTransactionsAsync()
        {
            if (_isFlushing || _pendingTransactions.Count == 0) return;
            _isFlushing = true;

            try
            {
                while (_pendingTransactions.Count > 0)
                {
                    var tx = _pendingTransactions[0];
                    var result = await _server.PostTransaction(
                        tx.Type, tx.Amount, tx.Reason, tx.ReferenceId);

                    if (result.IsSuccess)
                    {
                        _pendingTransactions.RemoveAt(0);
                        ApplyServerBalance(result.Data.NewBalance);
                    }
                    else if (result.WentOffline)
                    {
                        break; // Network dropped again — stop flushing
                    }
                    else
                    {
                        // Server rejected (insufficient funds, invalid, etc.) — discard
                        _pendingTransactions.RemoveAt(0);
                        Debug.LogWarning(
                            $"[HybridEconomy] Dropped queued {tx.Type} transaction " +
                            $"({tx.Amount} fragments, reason: {tx.Reason}): " +
                            $"{result.Error?.Code}");
                    }
                }

                // Reconcile local balance with server after flush
                await SyncBalanceAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HybridEconomy] Flush failed: {ex.Message}");
            }
            finally
            {
                _isFlushing = false;
            }
        }

        #endregion

        #region Internal

        async Task PostTransactionAsync(string type, int amount, string reason, string referenceId)
        {
            try
            {
                var result = await _server.PostTransaction(type, amount, reason, referenceId);
                if (result.IsSuccess)
                    ApplyServerBalance(result.Data.NewBalance);
                else if (result.HttpStatus is 422 or 400)
                    await SyncBalanceAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HybridEconomy] Background transaction failed: {ex.Message}");
            }
        }

        void ApplyServerBalance(int serverBalance)
        {
            _local.SetBalance(serverBalance);
        }

        void Enqueue(string type, int amount, string reason, string referenceId)
        {
            _pendingTransactions.Add(new PendingTransaction
            {
                Type = type,
                Amount = amount,
                Reason = reason,
                ReferenceId = referenceId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            // TODO: persist to SyncQueue when available (task 2.15)
        }

        void OnConnectivityChanged(bool isOnline)
        {
            if (isOnline)
                _ = FlushPendingTransactionsAsync();
        }

        #endregion

        #region Pending transaction model

        struct PendingTransaction
        {
            public string Type;
            public int Amount;
            public string Reason;
            public string ReferenceId;
            public long CreatedAt;
        }

        #endregion
    }
}
