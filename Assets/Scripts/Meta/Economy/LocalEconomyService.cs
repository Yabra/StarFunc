using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;

namespace StarFunc.Meta
{
    public class LocalEconomyService : IEconomyService
    {
        readonly ISaveService _saveService;
        readonly BalanceConfig _balanceConfig;
        readonly GameEvent<int> _onFragmentsChanged;
        readonly PlayerSaveData _save;

        public LocalEconomyService(
            ISaveService saveService,
            BalanceConfig balanceConfig,
            GameEvent<int> onFragmentsChanged)
        {
            _saveService = saveService;
            _balanceConfig = balanceConfig;
            _onFragmentsChanged = onFragmentsChanged;

            _save = _saveService.Load() ?? new PlayerSaveData();
        }

        public int GetFragments() => _save.TotalFragments;

        public void AddFragments(int amount)
        {
            if (amount <= 0) return;

            _save.TotalFragments += amount;
            SaveAndNotify();
        }

        public bool SpendFragments(int amount)
        {
            if (amount <= 0) return false;
            if (!CanAfford(amount)) return false;

            _save.TotalFragments -= amount;
            SaveAndNotify();
            return true;
        }

        public bool CanAfford(int amount) => _save.TotalFragments >= amount;

        /// <summary>
        /// Force-set balance from authoritative server value. Used by HybridEconomyService.
        /// </summary>
        public void SetBalance(int balance)
        {
            _save.TotalFragments = balance;
            SaveAndNotify();
        }

        public int CalculateImprovementBonus(int newStars, int previousBestStars)
        {
            int delta = newStars - previousBestStars;
            return delta > 0 ? delta * _balanceConfig.ImprovementBonusPerStar : 0;
        }

        void SaveAndNotify()
        {
            _save.IncrementVersion();
            _saveService.Save(_save);
            _onFragmentsChanged?.Raise(_save.TotalFragments);
        }
    }
}
