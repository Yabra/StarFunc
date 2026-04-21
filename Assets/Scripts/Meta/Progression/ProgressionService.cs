using System;
using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    public class ProgressionService : IProgressionService
    {
        const int ControlLevelIndex = 18;

        readonly ISaveService _saveService;
        readonly IEconomyService _economyService;
        readonly BalanceConfig _balanceConfig;
        readonly SectorData[] _sectors;
        readonly GameEvent<SectorData> _onSectorUnlocked;
        readonly GameEvent<SectorData> _onSectorCompleted;

        readonly PlayerSaveData _save;
        readonly Dictionary<string, SectorData> _sectorById = new();
        readonly Dictionary<string, (SectorData sector, LevelData level)> _levelLookup = new();

        public ProgressionService(
            ISaveService saveService,
            IEconomyService economyService,
            BalanceConfig balanceConfig,
            SectorData[] sectors,
            GameEvent<SectorData> onSectorUnlocked,
            GameEvent<SectorData> onSectorCompleted)
        {
            _saveService = saveService;
            _economyService = economyService;
            _balanceConfig = balanceConfig;
            _sectors = sectors;
            _onSectorUnlocked = onSectorUnlocked;
            _onSectorCompleted = onSectorCompleted;

            _save = _saveService.Load() ?? new PlayerSaveData();
            BuildLookups();
            EnsureInitialState();
        }

        void BuildLookups()
        {
            foreach (var sector in _sectors)
            {
                _sectorById[sector.SectorId] = sector;
                if (sector.Levels == null) continue;
                foreach (var level in sector.Levels)
                    _levelLookup[level.LevelId] = (sector, level);
            }
        }

        void EnsureInitialState()
        {
            foreach (var sector in _sectors)
            {
                if (sector.SectorIndex != 0) continue;
                var sp = GetOrCreateSectorProgress(sector.SectorId);
                if (sp.State == SectorState.Locked)
                    sp.State = SectorState.Available;
                break;
            }
        }

        // ===== Sector State =====

        public SectorState GetSectorState(string sectorId)
        {
            return _save.SectorProgress.TryGetValue(sectorId, out var sp)
                ? sp.State
                : SectorState.Locked;
        }

        public bool IsSectorUnlocked(string sectorId)
        {
            return GetSectorState(sectorId) != SectorState.Locked;
        }

        public bool IsSectorCompleted(string sectorId)
        {
            return GetSectorState(sectorId) == SectorState.Completed;
        }

        public void CompleteSector(string sectorId)
        {
            var sp = GetOrCreateSectorProgress(sectorId);
            if (sp.State == SectorState.Completed) return;

            sp.State = SectorState.Completed;

            _save.IncrementVersion();
            _saveService.Save(_save);

            if (_sectorById.TryGetValue(sectorId, out var sector))
                _onSectorCompleted?.Raise(sector);
        }

        // ===== Level State =====

        public bool IsLevelUnlocked(string levelId)
        {
            if (!_levelLookup.TryGetValue(levelId, out var entry)) return false;

            var (sector, level) = entry;

            if (!IsSectorUnlocked(sector.SectorId)) return false;

            // First level in an unlocked sector is always available
            if (level.LevelIndex == 0) return true;

            // Previous level completed → this level is unlocked
            var prevLevel = sector.Levels[level.LevelIndex - 1];
            if (IsLevelCompleted(prevLevel.LevelId)) return true;

            // Bonus levels are optional: completing the level before the bonus
            // also unlocks the level after the bonus
            if (prevLevel.Type == LevelType.Bonus && level.LevelIndex >= 2)
            {
                var beforeBonus = sector.Levels[level.LevelIndex - 2];
                if (IsLevelCompleted(beforeBonus.LevelId)) return true;
            }

            return false;
        }

        public bool IsLevelCompleted(string levelId)
        {
            return _save.LevelProgress.TryGetValue(levelId, out var lp) && lp.IsCompleted;
        }

        public int GetBestStars(string levelId)
        {
            return _save.LevelProgress.TryGetValue(levelId, out var lp) ? lp.BestStars : 0;
        }

        public void CompleteLevel(string levelId, LevelResult result)
        {
            if (!result.IsValid) return;
            if (!_levelLookup.TryGetValue(levelId, out var entry)) return;

            var (sector, level) = entry;
            var progress = GetOrCreateLevelProgress(levelId);

            bool isFirstCompletion = !progress.IsCompleted;
            int previousBestStars = progress.BestStars;

            // Update progress
            progress.IsCompleted = true;
            progress.BestStars = Math.Max(progress.BestStars, result.Stars);
            progress.Attempts++;

            // BestTime: handle 0 (from skip) — real time always overwrites zero
            if (result.Time > 0f)
                progress.BestTime = progress.BestTime > 0f
                    ? Math.Min(progress.BestTime, result.Time)
                    : result.Time;

            // Award fragments via EconomyService
            if (result.FragmentsEarned > 0)
                _economyService.AddFragments(result.FragmentsEarned);

            // Statistics
            if (isFirstCompletion)
                _save.TotalLevelsCompleted++;

            int starDelta = progress.BestStars - previousBestStars;
            if (starDelta > 0)
                _save.TotalStarsCollected += starDelta;

            // Sector-level bookkeeping (stars, state, unlock checks)
            UpdateSectorAfterLevel(sector, level);

            _save.IncrementVersion();
            _saveService.Save(_save);
        }

        // ===== Stars =====

        public int GetTotalStars()
        {
            return _save.TotalStarsCollected;
        }

        public int GetSectorStars(string sectorId)
        {
            return _save.SectorProgress.TryGetValue(sectorId, out var sp)
                ? sp.StarsCollected
                : 0;
        }

        // ===== Unlock Check =====

        public bool CanUnlockSector(string sectorId)
        {
            if (!_sectorById.TryGetValue(sectorId, out var sector)) return false;
            if (sector.PreviousSector == null) return true;

            var prev = sector.PreviousSector;
            if (!_save.SectorProgress.TryGetValue(prev.SectorId, out var prevProgress))
                return false;

            // Control level (index 18) of previous sector must be passed
            if (!prevProgress.ControlLevelPassed) return false;

            // Star threshold — bonus level stars excluded
            int nonBonusStars = GetSectorStarsExcludingBonus(prev);
            return nonBonusStars >= sector.RequiredStarsToUnlock;
        }

        // ===== Skip =====

        public bool CanSkipLevel(string levelId)
        {
            if (!_levelLookup.ContainsKey(levelId)) return false;
            if (IsLevelCompleted(levelId)) return false;
            if (!IsLevelUnlocked(levelId)) return false;
            return _economyService.CanAfford(_balanceConfig.SkipLevelCostFragments);
        }

        public void SkipLevel(string levelId)
        {
            if (!CanSkipLevel(levelId)) return;
            if (!_levelLookup.TryGetValue(levelId, out var entry)) return;

            var (sector, level) = entry;

            _economyService.SpendFragments(_balanceConfig.SkipLevelCostFragments);

            var progress = GetOrCreateLevelProgress(levelId);
            bool isFirstCompletion = !progress.IsCompleted;
            int previousBestStars = progress.BestStars;

            progress.IsCompleted = true;
            progress.BestStars = Math.Max(progress.BestStars, 1);
            // BestTime stays 0 for skipped levels

            if (isFirstCompletion)
                _save.TotalLevelsCompleted++;

            int starDelta = progress.BestStars - previousBestStars;
            if (starDelta > 0)
                _save.TotalStarsCollected += starDelta;

            UpdateSectorAfterLevel(sector, level);

            _save.IncrementVersion();
            _saveService.Save(_save);
        }

        // ===== Internal helpers =====

        void UpdateSectorAfterLevel(SectorData sector, LevelData level)
        {
            var sp = GetOrCreateSectorProgress(sector.SectorId);
            sp.StarsCollected = RecalculateSectorStars(sector);

            if (sp.State == SectorState.Available)
            {
                sp.State = SectorState.InProgress;
                if (sector.SectorIndex > _save.CurrentSectorIndex)
                    _save.CurrentSectorIndex = sector.SectorIndex;
            }

            if (level.LevelIndex == ControlLevelIndex)
                sp.ControlLevelPassed = true;

            // Final level (index 19) → sector completed
            if (level.Type == LevelType.Final && sp.State != SectorState.Completed)
            {
                sp.State = SectorState.Completed;
                _onSectorCompleted?.Raise(sector);
            }

            TryUnlockNextSectors();
        }

        void TryUnlockNextSectors()
        {
            foreach (var sector in _sectors)
            {
                if (IsSectorUnlocked(sector.SectorId)) continue;
                if (!CanUnlockSector(sector.SectorId)) continue;

                var sp = GetOrCreateSectorProgress(sector.SectorId);
                sp.State = SectorState.Available;
                _onSectorUnlocked?.Raise(sector);
            }
        }

        SectorProgress GetOrCreateSectorProgress(string sectorId)
        {
            if (!_save.SectorProgress.TryGetValue(sectorId, out var sp))
            {
                sp = new SectorProgress();
                _save.SectorProgress[sectorId] = sp;
            }
            return sp;
        }

        LevelProgress GetOrCreateLevelProgress(string levelId)
        {
            if (!_save.LevelProgress.TryGetValue(levelId, out var lp))
            {
                lp = new LevelProgress();
                _save.LevelProgress[levelId] = lp;
            }
            return lp;
        }

        int RecalculateSectorStars(SectorData sector)
        {
            int total = 0;
            if (sector.Levels == null) return total;
            foreach (var level in sector.Levels)
            {
                if (_save.LevelProgress.TryGetValue(level.LevelId, out var lp))
                    total += lp.BestStars;
            }
            return total;
        }

        int GetSectorStarsExcludingBonus(SectorData sector)
        {
            int total = 0;
            if (sector.Levels == null) return total;
            foreach (var level in sector.Levels)
            {
                if (level.Type == LevelType.Bonus) continue;
                if (_save.LevelProgress.TryGetValue(level.LevelId, out var lp))
                    total += lp.BestStars;
            }
            return total;
        }
    }
}
