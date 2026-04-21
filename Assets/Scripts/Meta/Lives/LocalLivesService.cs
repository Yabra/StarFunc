using System;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    public class LocalLivesService : ILivesService
    {
        readonly ISaveService _saveService;
        readonly BalanceConfig _balanceConfig;
        readonly IEconomyService _economyService;
        readonly GameEvent<int> _onLivesChanged;
        readonly PlayerSaveData _save;

        float _timeUntilNextRestore;

        public LocalLivesService(
            ISaveService saveService,
            BalanceConfig balanceConfig,
            IEconomyService economyService,
            GameEvent<int> onLivesChanged)
        {
            _saveService = saveService;
            _balanceConfig = balanceConfig;
            _economyService = economyService;
            _onLivesChanged = onLivesChanged;

            _save = _saveService.Load() ?? new PlayerSaveData();

            RecalculateAfterOffline();
        }

        public int GetCurrentLives() => _save.CurrentLives;

        public int GetMaxLives() => _balanceConfig.MaxLives;

        public bool HasLives() => _save.CurrentLives > 0;

        public float GetTimeUntilNextRestore()
        {
            if (_save.CurrentLives >= _balanceConfig.MaxLives)
                return 0f;

            return _timeUntilNextRestore;
        }

        public bool RestoreLife()
        {
            if (_save.CurrentLives >= _balanceConfig.MaxLives) return false;

            int cost = _balanceConfig.RestoreCostFragments;
            if (!_economyService.SpendFragments(cost)) return false;

            _save.CurrentLives++;
            ResetTimerIfAtMax();
            SaveAndNotify();
            return true;
        }

        public bool RestoreAllLives()
        {
            int missing = _balanceConfig.MaxLives - _save.CurrentLives;
            if (missing <= 0) return false;

            int totalCost = _balanceConfig.RestoreCostFragments * missing;
            if (!_economyService.SpendFragments(totalCost)) return false;

            _save.CurrentLives = _balanceConfig.MaxLives;
            ResetTimerIfAtMax();
            SaveAndNotify();
            return true;
        }

        /// <summary>
        /// Deducts one life. Called internally from LevelController on incorrect answer.
        /// Not part of the public ILivesService contract.
        /// </summary>
        internal void DeductLife()
        {
            if (_save.CurrentLives <= 0) return;

            bool wasAtMax = _save.CurrentLives >= _balanceConfig.MaxLives;
            _save.CurrentLives--;

            if (wasAtMax)
            {
                _timeUntilNextRestore = _balanceConfig.RestoreIntervalSeconds;
                _save.LastLifeRestoreTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            SaveAndNotify();
        }

        /// <summary>
        /// Must be called every frame (e.g. from a MonoBehaviour Update loop)
        /// to drive the auto-restore timer.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_save.CurrentLives >= _balanceConfig.MaxLives) return;

            _timeUntilNextRestore -= deltaTime;

            while (_timeUntilNextRestore <= 0f && _save.CurrentLives < _balanceConfig.MaxLives)
            {
                _save.CurrentLives++;
                _save.LastLifeRestoreTimestamp += _balanceConfig.RestoreIntervalSeconds;

                if (_save.CurrentLives < _balanceConfig.MaxLives)
                {
                    _timeUntilNextRestore += _balanceConfig.RestoreIntervalSeconds;
                }
                else
                {
                    _timeUntilNextRestore = 0f;
                }

                SaveAndNotify();
            }
        }

        void RecalculateAfterOffline()
        {
            if (_save.CurrentLives >= _balanceConfig.MaxLives)
            {
                _timeUntilNextRestore = 0f;
                return;
            }

            if (_save.LastLifeRestoreTimestamp <= 0)
            {
                _save.CurrentLives = _balanceConfig.MaxLives;
                _timeUntilNextRestore = 0f;
                SaveAndNotify();
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - _save.LastLifeRestoreTimestamp;

            if (elapsed <= 0)
            {
                _timeUntilNextRestore = _balanceConfig.RestoreIntervalSeconds;
                return;
            }

            int livesToRestore = (int)(elapsed / _balanceConfig.RestoreIntervalSeconds);
            int newLives = Math.Min(
                _save.CurrentLives + livesToRestore,
                _balanceConfig.MaxLives);

            if (newLives != _save.CurrentLives)
            {
                _save.CurrentLives = newLives;
                _save.LastLifeRestoreTimestamp += livesToRestore * _balanceConfig.RestoreIntervalSeconds;
            }

            if (_save.CurrentLives < _balanceConfig.MaxLives)
            {
                long remainderSeconds = elapsed % _balanceConfig.RestoreIntervalSeconds;
                _timeUntilNextRestore = _balanceConfig.RestoreIntervalSeconds - remainderSeconds;
            }
            else
            {
                _timeUntilNextRestore = 0f;
            }

            SaveAndNotify();
        }

        /// <summary>
        /// Force-set lives state from authoritative server value. Used by HybridLivesService.
        /// </summary>
        internal void SetState(int currentLives, float secondsUntilNextRestore, long lastLifeRestoreTimestamp)
        {
            _save.CurrentLives = Math.Min(currentLives, _balanceConfig.MaxLives);
            _save.LastLifeRestoreTimestamp = lastLifeRestoreTimestamp;
            _timeUntilNextRestore = _save.CurrentLives >= _balanceConfig.MaxLives
                ? 0f
                : secondsUntilNextRestore;
            SaveAndNotify();
        }

        void ResetTimerIfAtMax()
        {
            if (_save.CurrentLives >= _balanceConfig.MaxLives)
            {
                _timeUntilNextRestore = 0f;
                _save.LastLifeRestoreTimestamp = 0;
            }
        }

        void SaveAndNotify()
        {
            _save.IncrementVersion();
            _saveService.Save(_save);
            _onLivesChanged?.Raise(_save.CurrentLives);
        }
    }
}
