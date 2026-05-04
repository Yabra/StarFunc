using System;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    /// <summary>
    /// Listens for one-shot notification triggers (sector unlocks, lives going
    /// from 0 → positive) and exposes red-dot badge state for the hub.
    /// "Seen" content IDs are persisted in <see cref="PlayerSaveData.SeenContent"/>
    /// so badges survive app restarts until the player acknowledges them.
    /// </summary>
    public class NotificationService : INotificationService
    {
        readonly ISaveService _saveService;
        readonly IProgressionService _progression;
        readonly SectorData[] _sectors;
        readonly GameEvent<SectorData> _onSectorUnlocked;
        readonly GameEvent<int> _onLivesChanged;
        readonly PlayerSaveData _save;

        bool _livesRefilledPending;
        int _lastLivesSeen;

        public event Action OnChanged;

        public NotificationService(
            ISaveService saveService,
            IProgressionService progression,
            SectorData[] sectors,
            GameEvent<SectorData> onSectorUnlocked,
            GameEvent<int> onLivesChanged)
        {
            _saveService = saveService;
            _progression = progression;
            _sectors = sectors ?? Array.Empty<SectorData>();
            _onSectorUnlocked = onSectorUnlocked;
            _onLivesChanged = onLivesChanged;

            _save = _saveService.Load() ?? new PlayerSaveData();
            _save.SeenContent ??= new System.Collections.Generic.List<string>();
            _lastLivesSeen = _save.CurrentLives;

            if (_onSectorUnlocked) _onSectorUnlocked.AddListener(OnSectorUnlockedRaised);
            if (_onLivesChanged) _onLivesChanged.AddListener(OnLivesChangedRaised);
        }

        public bool HasNewContent(string sectorId)
        {
            if (string.IsNullOrEmpty(sectorId)) return false;
            if (_progression == null || !_progression.IsSectorUnlocked(sectorId)) return false;
            return !_save.SeenContent.Contains(INotificationService.SectorUnlockId(sectorId));
        }

        public bool HasUnclaimedRewards()
        {
            // Phase 4 hooks: shop deliveries, daily rewards, etc.
            return _livesRefilledPending && !_save.SeenContent.Contains(INotificationService.LivesRefilledId);
        }

        public void MarkSeen(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return;
            if (_save.SeenContent.Contains(contentId)) return;

            _save.SeenContent.Add(contentId);

            if (contentId == INotificationService.LivesRefilledId)
                _livesRefilledPending = false;

            _save.IncrementVersion();
            _saveService.Save(_save);
            OnChanged?.Invoke();
        }

        public int GetBadgeCount(string context)
        {
            if (context == "hub")
            {
                int count = 0;
                foreach (var s in _sectors)
                    if (s != null && HasNewContent(s.SectorId)) count++;
                if (HasUnclaimedRewards()) count++;
                return count;
            }
            return 0;
        }

        void OnSectorUnlockedRaised(SectorData sector)
        {
            // ProgressionService already persisted the unlock; we just clear any
            // stale "seen" entry from a previous lock cycle (rare) and notify UI.
            if (sector == null) return;
            _save.SeenContent.Remove(INotificationService.SectorUnlockId(sector.SectorId));

            // Clearing a seen-entry counts as a save-mutation worth persisting
            // so the badge remains across a hard kill before the next save.
            _save.IncrementVersion();
            _saveService.Save(_save);

            OnChanged?.Invoke();
        }

        void OnLivesChangedRaised(int currentLives)
        {
            // Lives went from 0 → positive: surface a badge until the player
            // returns to the hub and we mark it seen.
            if (_lastLivesSeen == 0 && currentLives > 0)
            {
                _livesRefilledPending = true;
                _save.SeenContent.Remove(INotificationService.LivesRefilledId);
                OnChanged?.Invoke();
            }
            _lastLivesSeen = currentLives;
        }
    }
}
