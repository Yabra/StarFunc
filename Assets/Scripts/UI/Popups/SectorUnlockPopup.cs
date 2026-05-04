using DG.Tweening;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Celebratory popup for "new sector unlocked" — shows the sector name and
    /// animated reveal (DOTween scale-in + fade). Caller passes a
    /// <see cref="SectorData"/> via <see cref="Show(SectorData)"/>.
    /// Hook this to the <c>OnSectorUnlocked</c> SO event from a Hub-side
    /// listener (Task 3.7 / NotificationService territory).
    /// </summary>
    public class SectorUnlockPopup : UIPopup
    {
        [Header("Labels")]
        [SerializeField] TMP_Text _titleText;
        [SerializeField] TMP_Text _sectorNameText;

        [Header("Visuals")]
        [SerializeField] Image _accentImage;
        [SerializeField] RectTransform _animatedRoot;

        [Header("Buttons")]
        [SerializeField] Button _continueButton;

        [Header("Animation")]
        [SerializeField] float _revealDuration = 0.5f;
        [SerializeField] float _revealStartScale = 0.6f;
        [SerializeField] Ease _revealEase = Ease.OutBack;

        Tween _revealTween;

        void Awake()
        {
            if (_continueButton) _continueButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_continueButton) _continueButton.onClick.RemoveListener(Hide);
            KillTween();
        }

        /// <summary>Show with sector context — drives labels and accent color.</summary>
        public void Show(SectorData sector)
        {
            base.Show(null);

            if (sector != null)
            {
                if (_titleText) _titleText.text = "Новый сектор открыт!";
                if (_sectorNameText) _sectorNameText.text = sector.DisplayName;
                if (_accentImage) _accentImage.color = sector.AccentColor;
            }

            PlayReveal();
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            PlayReveal();
        }

        public override void Hide()
        {
            KillTween();
            base.Hide();
        }

        void PlayReveal()
        {
            KillTween();
            if (_animatedRoot == null) return;

            _animatedRoot.localScale = Vector3.one * _revealStartScale;
            _revealTween = _animatedRoot
                .DOScale(1f, _revealDuration)
                .SetEase(_revealEase);

            // SFX + VFX cue — VFX prefab is spawned at world origin; its own
            // visual is a particle burst that does not need a precise screen
            // mapping (task 4.6).
            if (ServiceLocator.Contains<IFeedbackService>())
                ServiceLocator.Get<IFeedbackService>().PlayFeedback(FeedbackType.SectorUnlock);
        }

        void KillTween()
        {
            if (_revealTween != null && _revealTween.IsActive())
                _revealTween.Kill();
            _revealTween = null;
        }
    }
}
