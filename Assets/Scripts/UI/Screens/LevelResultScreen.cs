using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class LevelResultScreen : UIScreen
    {
        [Header("Widgets")]
        [SerializeField] StarRatingDisplay _starRating;
        [SerializeField] FragmentsDisplay _fragmentsDisplay;

        [Header("Labels")]
        [SerializeField] TMP_Text _timeText;
        [SerializeField] TMP_Text _titleText;

        [Header("Constellation Preview")]
        [SerializeField] Image _constellationPreview;

        [Header("Buttons")]
        [SerializeField] Button _nextButton;
        [SerializeField] Button _retryButton;
        [SerializeField] Button _hubButton;

        /// <summary>Raised when the player taps "Next Level".</summary>
        public event Action OnNextClicked;

        /// <summary>Raised when the player taps "Retry".</summary>
        public event Action OnRetryClicked;

        /// <summary>Raised when the player taps "To Hub".</summary>
        public event Action OnHubClicked;

        void Awake()
        {
            if (_nextButton)
                _nextButton.onClick.AddListener(() => OnNextClicked?.Invoke());

            if (_retryButton)
                _retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());

            if (_hubButton)
                _hubButton.onClick.AddListener(() => OnHubClicked?.Invoke());
        }

        public void Setup(LevelResult result)
        {
            if (result == null) return;

            // Endless-mode levels don't award persistent stars (story-mode
            // entity), so the rating row is hidden and the title reflects the
            // mode. The fragments display still shows the post-reward balance.
            bool isEphemeral = LevelData.ActiveLevel != null
                               && LevelData.ActiveLevel.IsEphemeral;

            if (_starRating)
            {
                if (isEphemeral)
                    _starRating.gameObject.SetActive(false);
                else
                    _starRating.SetStars(result.Stars, animate: true);
            }

            if (_fragmentsDisplay)
            {
                // Show the player's current TOTAL fragments, not just the reward
                // for this level. ProgressionService.CompleteLevel has already
                // run before HandleCompleted fires, so EconomyService reflects
                // the post-reward balance.
                int total = ServiceLocator.Contains<IEconomyService>()
                    ? ServiceLocator.Get<IEconomyService>().GetFragments()
                    : result.FragmentsEarned;
                _fragmentsDisplay.SetFragments(total);
            }

            if (_timeText)
            {
                int minutes = Mathf.FloorToInt(result.Time / 60f);
                int seconds = Mathf.FloorToInt(result.Time % 60f);
                _timeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_titleText)
            {
                _titleText.text = result.LevelFailed
                    ? "Уровень не пройден"
                    : (isEphemeral ? "Бесконечный уровень пройден!" : "Уровень пройден!");
            }

            // Disable "Next" when the level was failed. Endless levels also
            // disable "Next" — there is no curated next level; "Hub" sends
            // the player back to roll another one from the menu.
            if (_nextButton)
                _nextButton.interactable = !result.LevelFailed && !isEphemeral;
        }

        public void SetConstellationPreview(Sprite preview)
        {
            if (_constellationPreview && preview)
            {
                _constellationPreview.sprite = preview;
                _constellationPreview.gameObject.SetActive(true);
            }
        }
    }
}
