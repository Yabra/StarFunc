using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// "No lives" popup. Shows the countdown to the next auto-restore plus three
    /// actions: restore one (cost = <c>RestoreCostFragments</c>), restore all
    /// (cost = <c>RestoreCostFragments × (max - current)</c>, API.md §6.4
    /// <c>POST /lives/restore-all</c>), or wait. Auto-hides if lives become
    /// available while open.
    /// </summary>
    public class NoLivesPopup : UIPopup
    {
        [Header("Config")]
        [SerializeField] BalanceConfig _balanceConfig;

        [Header("Labels")]
        [SerializeField] TMP_Text _timerText;
        [SerializeField] TMP_Text _restoreOneCostText;
        [SerializeField] TMP_Text _restoreAllCostText;

        [Header("Buttons")]
        [SerializeField] Button _restoreOneButton;
        [SerializeField] Button _restoreAllButton;
        [SerializeField] Button _waitButton;

        ILivesService _lives;
        IEconomyService _economy;

        void Awake()
        {
            if (_restoreOneButton) _restoreOneButton.onClick.AddListener(OnRestoreOne);
            if (_restoreAllButton) _restoreAllButton.onClick.AddListener(OnRestoreAll);
            if (_waitButton) _waitButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_restoreOneButton) _restoreOneButton.onClick.RemoveListener(OnRestoreOne);
            if (_restoreAllButton) _restoreAllButton.onClick.RemoveListener(OnRestoreAll);
            if (_waitButton) _waitButton.onClick.RemoveListener(Hide);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            ResolveServices();
            UpdateDisplay();
        }

        /// <summary>Show without PopupData.</summary>
        public void Show() => Show((PopupData)null);

        void Update()
        {
            if (!IsVisible) return;
            UpdateDisplay();

            // Lives may regenerate while the popup is open — auto-close.
            if (_lives != null && _lives.HasLives())
                Hide();
        }

        void ResolveServices()
        {
            if (_lives == null && ServiceLocator.Contains<ILivesService>())
                _lives = ServiceLocator.Get<ILivesService>();
            if (_economy == null && ServiceLocator.Contains<IEconomyService>())
                _economy = ServiceLocator.Get<IEconomyService>();
        }

        void UpdateDisplay()
        {
            if (_lives == null) return;

            int current = _lives.GetCurrentLives();
            int max = _lives.GetMaxLives();
            int missing = Mathf.Max(0, max - current);

            int oneCost = _balanceConfig != null ? _balanceConfig.RestoreCostFragments : 0;
            int allCost = oneCost * missing;

            if (_timerText)
            {
                float seconds = _lives.GetTimeUntilNextRestore();
                if (seconds > 0f)
                {
                    int m = Mathf.FloorToInt(seconds / 60f);
                    int s = Mathf.FloorToInt(seconds % 60f);
                    _timerText.text = $"{m:00}:{s:00}";
                }
                else
                {
                    _timerText.text = "--:--";
                }
            }

            if (_restoreOneCostText) _restoreOneCostText.text = oneCost.ToString();
            if (_restoreAllCostText) _restoreAllCostText.text = allCost.ToString();

            // Gate buttons by affordability and lives state.
            if (_restoreOneButton)
                _restoreOneButton.interactable = missing > 0
                    && _economy != null && _economy.CanAfford(oneCost);

            if (_restoreAllButton)
                _restoreAllButton.interactable = missing > 0
                    && _economy != null && _economy.CanAfford(allCost);
        }

        void OnRestoreOne()
        {
            if (_lives == null) return;
            if (_lives.RestoreLife())
            {
                Debug.Log("[NoLivesPopup] Restored one life.");
                if (_lives.HasLives()) Hide();
                else UpdateDisplay();
            }
            else
            {
                Debug.Log("[NoLivesPopup] RestoreLife failed (insufficient fragments or already at max).");
            }
        }

        void OnRestoreAll()
        {
            if (_lives == null) return;
            if (_lives.RestoreAllLives())
            {
                Debug.Log("[NoLivesPopup] Restored all lives.");
                Hide();
            }
            else
            {
                Debug.Log("[NoLivesPopup] RestoreAllLives failed (insufficient fragments or already at max).");
            }
        }
    }
}
