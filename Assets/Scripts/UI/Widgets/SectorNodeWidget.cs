using System;
using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class SectorNodeWidget : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] Image _sectorIcon;
        [SerializeField] Image _stateRing;
        [SerializeField] Image _connectionLine;
        [SerializeField] GameObject _lockOverlay;
        [SerializeField] GameObject _notificationBadge;
        [SerializeField] TMP_Text _sectorNameText;
        [SerializeField] TMP_Text _starsText;

        [Header("Interaction")]
        [SerializeField] Button _button;

        SectorData _sectorData;
        SectorState _state;

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
        }

        public void UpdateState(SectorState state, int starsCollected)
        {
            _state = state;

            if (_starsText)
                _starsText.text = state == SectorState.Locked ? "" : $"★ {starsCollected}";

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
                _sectorIcon.color = state switch
                {
                    SectorState.Locked => new Color(0.3f, 0.3f, 0.3f, 0.5f),
                    SectorState.Completed => Color.white,
                    _ => Color.white
                };
            }

            if (_stateRing && _sectorData != null)
            {
                _stateRing.color = state switch
                {
                    SectorState.Locked => new Color(0.3f, 0.3f, 0.3f, 0.3f),
                    SectorState.Available => _sectorData.AccentColor,
                    SectorState.InProgress => _sectorData.AccentColor,
                    SectorState.Completed => new Color(1f, 0.84f, 0f),
                    _ => Color.white
                };
            }

            // Notification badge — stub for Phase 3
            if (_notificationBadge)
                _notificationBadge.SetActive(false);
        }

        public void SetConnectionLineColor(Color color)
        {
            if (_connectionLine)
                _connectionLine.color = color;
        }

        void HandleClick()
        {
            if (_state == SectorState.Available || _state == SectorState.InProgress)
                OnClicked?.Invoke(_sectorData);
        }

        void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
