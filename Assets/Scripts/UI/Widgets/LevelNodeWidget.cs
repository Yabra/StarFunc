using System;
using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public enum LevelNodeState
    {
        Locked,
        Available,
        Completed
    }

    public class LevelNodeWidget : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] TMP_Text _levelNumberText;
        [SerializeField] Image _nodeIcon;
        [SerializeField] Image[] _starImages;
        [SerializeField] Sprite _starFilled;
        [SerializeField] Sprite _starEmpty;
        [SerializeField] GameObject _lockOverlay;
        [SerializeField] Image _connectionLine;

        [Header("State Sprites")]
        [SerializeField] Sprite _lockedSprite;
        [SerializeField] Sprite _availableSprite;
        [SerializeField] Sprite _completedSprite;

        [Header("Colors")]
        [SerializeField] Color _lockedColor = new(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] Color _availableColor = Color.white;
        [SerializeField] Color _completedColor = new(1f, 0.84f, 0f);

        [Header("Interaction")]
        [SerializeField] Button _button;

        LevelData _levelData;
        LevelNodeState _state;

        public event Action<LevelData> OnClicked;
        public LevelNodeState State => _state;

        void Awake()
        {
            if (_button)
                _button.onClick.AddListener(HandleClick);
        }

        public void Setup(LevelData level, int displayNumber, LevelNodeState state, int bestStars)
        {
            _levelData = level;
            _state = state;

            if (_levelNumberText)
                _levelNumberText.text = displayNumber.ToString();

            ApplyState(state, bestStars);
        }

        public void SetConnectionLineVisible(bool visible)
        {
            if (_connectionLine)
                _connectionLine.gameObject.SetActive(visible);
        }

        public void SetConnectionLineColor(Color color)
        {
            if (_connectionLine)
                _connectionLine.color = color;
        }

        void ApplyState(LevelNodeState state, int bestStars)
        {
            bool interactable = state != LevelNodeState.Locked;

            if (_button)
                _button.interactable = interactable;

            if (_lockOverlay)
                _lockOverlay.SetActive(state == LevelNodeState.Locked);

            if (_nodeIcon)
            {
                _nodeIcon.sprite = state switch
                {
                    LevelNodeState.Locked => _lockedSprite,
                    LevelNodeState.Available => _availableSprite,
                    LevelNodeState.Completed => _completedSprite,
                    _ => _availableSprite
                };

                _nodeIcon.color = state switch
                {
                    LevelNodeState.Locked => _lockedColor,
                    LevelNodeState.Available => _availableColor,
                    LevelNodeState.Completed => _completedColor,
                    _ => _availableColor
                };
            }

            UpdateStars(state == LevelNodeState.Completed ? bestStars : 0);

            if (_levelNumberText)
                _levelNumberText.color = state == LevelNodeState.Locked
                    ? new Color(1f, 1f, 1f, 0.3f)
                    : Color.white;
        }

        void UpdateStars(int count)
        {
            if (_starImages == null) return;

            bool showStars = _state == LevelNodeState.Completed;

            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].gameObject.SetActive(showStars);
                _starImages[i].sprite = i < count ? _starFilled : _starEmpty;
            }
        }

        void HandleClick()
        {
            if (_state != LevelNodeState.Locked && _levelData != null)
                OnClicked?.Invoke(_levelData);
        }

        void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
