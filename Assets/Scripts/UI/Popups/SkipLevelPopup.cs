using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Skip-level confirmation popup. Spends <c>SkipLevelCostFragments</c> via
    /// <see cref="IProgressionService.SkipLevel"/>, which marks the level as
    /// completed with 1 star and zero fragment reward.
    /// Caller seeds the level id with <see cref="Show(string)"/>.
    /// </summary>
    public class SkipLevelPopup : UIPopup
    {
        [Header("Config")]
        [SerializeField] BalanceConfig _balanceConfig;

        [Header("Labels")]
        [SerializeField] TMP_Text _costText;
        [SerializeField] TMP_Text _warningText;

        [Header("Buttons")]
        [SerializeField] Button _skipButton;
        [SerializeField] Button _cancelButton;

        IProgressionService _progression;
        IEconomyService _economy;
        string _levelId;

        void Awake()
        {
            if (_skipButton) _skipButton.onClick.AddListener(OnSkip);
            if (_cancelButton) _cancelButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_skipButton) _skipButton.onClick.RemoveListener(OnSkip);
            if (_cancelButton) _cancelButton.onClick.RemoveListener(Hide);
        }

        /// <summary>Show the popup configured for skipping <paramref name="levelId"/>.</summary>
        public void Show(string levelId)
        {
            _levelId = levelId;
            Show((PopupData)null);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            ResolveServices();
            UpdateDisplay();
        }

        void ResolveServices()
        {
            if (_progression == null && ServiceLocator.Contains<IProgressionService>())
                _progression = ServiceLocator.Get<IProgressionService>();
            if (_economy == null && ServiceLocator.Contains<IEconomyService>())
                _economy = ServiceLocator.Get<IEconomyService>();
        }

        void UpdateDisplay()
        {
            int cost = _balanceConfig != null ? _balanceConfig.SkipLevelCostFragments : 0;
            if (_costText) _costText.text = cost.ToString();

            // Warning text is static and configured in the inspector — leaving the
            // hook here so designers can keep "1 звезда, без награды" in one place.
            if (_warningText && string.IsNullOrEmpty(_warningText.text))
                _warningText.text = "1 звезда, без награды за фрагменты.";

            // Gate the Skip button: must be affordable, level must exist, must
            // not already be completed.
            bool canSkip = !string.IsNullOrEmpty(_levelId)
                           && _progression != null
                           && _progression.CanSkipLevel(_levelId);

            if (_skipButton) _skipButton.interactable = canSkip;
        }

        void OnSkip()
        {
            if (string.IsNullOrEmpty(_levelId) || _progression == null) return;
            if (!_progression.CanSkipLevel(_levelId))
            {
                Debug.Log($"[SkipLevelPopup] CanSkipLevel('{_levelId}') = false; aborting.");
                return;
            }

            _progression.SkipLevel(_levelId);
            Debug.Log($"[SkipLevelPopup] Skipped level '{_levelId}'.");
            Hide();
        }
    }
}
