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

        [Header("Interaction")]
        [SerializeField] Button _button;

        SectorData _sectorData;
        SectorState _state;
        Tween _badgeTween;

        public event Action<SectorData> OnClicked;
        public SectorData SectorData => _sectorData;

        void Awake()
        {
            if (_button)
                _button.onClick.AddListener(HandleClick);
        }

        public void Setup(SectorData data, SectorState state, int starsCollected)
        {
            _sectorData = data;
            UpdateState(state, starsCollected);

            if (_sectorNameText)
                _sectorNameText.text = data.DisplayName;

            if (_sectorIcon && data.SectorIcon)
                _sectorIcon.sprite = data.SectorIcon;

            // Constellation sprite is picked in ApplyVisualState (driven by
            // state) so the swap to the "restored" art happens automatically
            // when the sector transitions to Completed.
        }

        public void UpdateState(SectorState state, int starsCollected)
        {
            _state = state;

            // Glyph (★) lives in a sibling Image now — text holds just the number.
            if (_starsText)
                _starsText.text = state == SectorState.Locked ? "" : starsCollected.ToString();

            ApplyVisualState(state);
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
