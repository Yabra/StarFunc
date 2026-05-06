using System;
using DG.Tweening;
using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class SectorNodeWidget : MonoBehaviour
    {
        const float BadgeAppearDuration = 0.35f;
        [Header("Visual")]
        [SerializeField] Image _sectorIcon;
        [SerializeField] Image _stateRing;
        [SerializeField] Image _sectorConstellation;
        [SerializeField] GameObject _lockOverlay;
        [SerializeField] GameObject _notificationBadge;
        [SerializeField] TMP_Text _sectorNameText;
        [SerializeField] TMP_Text _starsText;
        [Tooltip("Stars-glyph image that sits next to _starsText. Hidden " +
                 "automatically when _starsText is empty/disabled — locked " +
                 "sectors blank the count, and we don't want a lonely icon " +
                 "floating next to nothing.")]
        [SerializeField] GameObject _starsIcon;
        [Tooltip("Empty child Transform (SectorLineAnchor) marking where " +
                 "hub bezier connections should attach. Designers position " +
                 "this point per-sector so the curves meet exactly where " +
                 "the art demands. Falls back to the sector icon, then to " +
                 "the SectorNode root if not assigned.")]
        [SerializeField] Transform _sectorLineAnchor;

        [Header("Interaction")]
        [SerializeField] Button _button;

        SectorData _sectorData;
        SectorState _state;
        int _levelsCompleted;
        int _levelsTotal;
        Tween _badgeTween;

        public event Action<SectorData> OnClicked;
        public SectorData SectorData => _sectorData;

        /// <summary>
        /// Attachment point for hub bezier connections. Resolves in this
        /// order: explicit <see cref="_sectorLineAnchor"/> child → sector
        /// icon (legacy fallback) → SectorNode root.
        /// </summary>
        public Transform LineAnchorTransform =>
            _sectorLineAnchor != null ? _sectorLineAnchor
            : _sectorIcon != null ? _sectorIcon.transform
            : transform;

        void Awake()
        {
            if (_button)
                _button.onClick.AddListener(HandleClick);
        }

        public void Setup(SectorData data, SectorState state, int starsCollected,
            int levelsCompleted = 0, int levelsTotal = 0)
        {
            _sectorData = data;
            UpdateState(state, starsCollected, levelsCompleted, levelsTotal);

            if (_sectorNameText)
                _sectorNameText.text = data.DisplayName;

            if (_sectorIcon && data.SectorIcon)
                _sectorIcon.sprite = data.SectorIcon;

            // Constellation sprite is picked in ApplyVisualState (driven by
            // state) so the swap to the "restored" art happens automatically
            // when the sector transitions to Completed.
        }

        public void UpdateState(SectorState state, int starsCollected,
            int levelsCompleted = 0, int levelsTotal = 0)
        {
            _state = state;
            _levelsCompleted = levelsCompleted;
            _levelsTotal = levelsTotal;

            // Glyph (★) lives in a sibling Image now — text holds just the number.
            if (_starsText)
                _starsText.text = state == SectorState.Locked ? "" : starsCollected.ToString();

            UpdateStarsIcon();
            ApplyVisualState(state);
        }

        void UpdateStarsIcon()
        {
            if (_starsIcon == null) return;

            // Show the star glyph only when the count text is going to be
            // visible *and* contains something. Locked sectors blank the
            // text, and a designer can also disable the text component or
            // its GameObject — match either path.
            bool show = _starsText != null
                        && _starsText.gameObject.activeInHierarchy
                        && _starsText.enabled
                        && !string.IsNullOrEmpty(_starsText.text);

            _starsIcon.SetActive(show);
        }

        void ApplyVisualState(SectorState state)
        {
            bool interactable = state == SectorState.Available || state == SectorState.InProgress;

            if (_button)
                _button.interactable = interactable;

            if (_lockOverlay)
                _lockOverlay.SetActive(state == SectorState.Locked);

            if (_sectorIcon)
            {
                // Locked sectors are silhouetted by the lock overlay; the
                // sector's own icon is hidden so it doesn't poke through.
                // Completed sectors fade the icon down so the restored
                // constellation reads as the focal element of the node.
                _sectorIcon.enabled = state != SectorState.Locked;
                _sectorIcon.color = state == SectorState.Completed
                    ? new Color(1f, 1f, 1f, 0.75f)
                    : Color.white;
            }

            if (_sectorConstellation)
            {
                _sectorConstellation.enabled = state != SectorState.Locked;
                if (_sectorData != null)
                {
                    // Completed sectors reveal the "restored" constellation
                    // (the lit-up version of the same shape). Fall back to
                    // the base sprite if the designer hasn't authored a
                    // restored variant yet.
                    var restored = _sectorData.ConstellationRestoredSprite;
                    var baseSprite = _sectorData.ConstellationSprite;
                    _sectorConstellation.sprite = state == SectorState.Completed
                        ? (restored != null ? restored : baseSprite)
                        : baseSprite;
                }
            }

            if (_stateRing && _sectorData != null)
            {
                _stateRing.color = state switch
                {
                    SectorState.Locked => new Color(0.3f, 0.3f, 0.3f, 0.3f),
                    SectorState.Available => _sectorData.AccentColor,
                    SectorState.InProgress => _sectorData.AccentColor,
                    // Completed sectors hide the state ring entirely so the
                    // restored constellation sits centre-stage.
                    SectorState.Completed => new Color(1f, 1f, 1f, 0f),
                    _ => Color.white
                };

                // Ring fills around the node based on level-completion
                // progress. Locked/Available start empty; Completed shows
                // a full ring (alpha=0 hides it visually anyway, but we
                // still fill so a designer flipping the alpha back on can
                // verify the state is "done"). The Image's Type must be
                // set to Filled (Radial360 / Vertical / etc.) in the
                // prefab for fillAmount to render.
                _stateRing.fillAmount = state switch
                {
                    SectorState.Locked => 0f,
                    SectorState.Completed => 1f,
                    _ => _levelsTotal > 0
                        ? Mathf.Clamp01((float)_levelsCompleted / _levelsTotal)
                        : 0f,
                };
            }

            // Default: no badge until SetBadge() is called by the hub.
            if (_notificationBadge)
                _notificationBadge.SetActive(false);
        }

        /// <summary>
        /// Show or hide the red-dot notification badge. Pop-in animation uses
        /// DOTween scale (0 → 1, OutBack); hide is instant.
        /// </summary>
        public void SetBadge(bool show)
        {
            if (_notificationBadge == null) return;

            KillBadgeTween();

            if (show)
            {
                bool wasVisible = _notificationBadge.activeSelf;
                _notificationBadge.SetActive(true);

                if (!wasVisible)
                {
                    var t = _notificationBadge.transform;
                    t.localScale = Vector3.zero;
                    _badgeTween = t.DOScale(1f, BadgeAppearDuration).SetEase(Ease.OutBack);
                }
            }
            else
            {
                _notificationBadge.transform.localScale = Vector3.one;
                _notificationBadge.SetActive(false);
            }
        }

        void KillBadgeTween()
        {
            if (_badgeTween != null && _badgeTween.IsActive())
                _badgeTween.Kill();
            _badgeTween = null;
        }

        void HandleClick()
        {
            if (_state == SectorState.Available || _state == SectorState.InProgress)
                OnClicked?.Invoke(_sectorData);
        }

        void OnDestroy()
        {
            KillBadgeTween();
            if (_button)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
