using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Gameplay;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class LevelHUD : UIScreen
    {
        [Header("Widgets")]
        [SerializeField] TimerDisplay _timerDisplay;
        [SerializeField] LivesDisplay _livesDisplay;
        [SerializeField] AnswerPanel _answerPanel;

        [Header("Level Controller")]
        [SerializeField] LevelController _levelController;
        [SerializeField] HintSystem _hintSystem;

        [Header("Buttons")]
        [SerializeField] Button _pauseButton;
        [SerializeField] Button _confirmButton;
        [SerializeField] Button _undoButton;
        [SerializeField] Button _resetButton;
        [SerializeField] Button _hintButton;

        [Header("Hints UI")]
        [Tooltip("Number badge next to the hint button — shows paid-hint inventory.")]
        [SerializeField] TMP_Text _hintsAmountText;
        [Tooltip("Raised when Consumables['hints'] changes (HintSystem on use, ShopService on purchase).")]
        [SerializeField] GameEvent<int> _onHintsChanged;

        bool _isPaused;
        bool _hintsEventSubscribed;

        void Start()
        {
            if (_levelController == null)
            {
                Debug.LogError("[LevelHUD] LevelController reference is missing.");
                return;
            }

            InitializeWidgets();
            BindButtons();
            InitializeHintsUI();
        }

        void InitializeWidgets()
        {
            if (_timerDisplay)
                _timerDisplay.Initialize(_levelController);

            // LivesDisplay self-initializes from ILivesService and listens to
            // OnLivesChanged for updates — no need to push a hard-coded count.

            if (_answerPanel && _levelController.AnswerSystem)
                _answerPanel.Initialize(_levelController.AnswerSystem);
        }

        void BindButtons()
        {
            if (_confirmButton)
                _confirmButton.onClick.AddListener(OnConfirmClicked);

            if (_undoButton)
                _undoButton.onClick.AddListener(OnUndoClicked);

            if (_resetButton)
                _resetButton.onClick.AddListener(OnResetClicked);

            if (_pauseButton)
                _pauseButton.onClick.AddListener(OnPauseClicked);

            if (_hintButton)
            {
                if (_hintSystem != null)
                    _hintButton.onClick.AddListener(OnHintClicked);
                else
                    _hintButton.interactable = false;
            }
        }

        void InitializeHintsUI()
        {
            int initial = ServiceLocator.Contains<IShopService>()
                ? ServiceLocator.Get<IShopService>().GetConsumableCount(LocalShopService.HintsKey)
                : 0;
            RefreshHintsCount(initial);

            if (_onHintsChanged && !_hintsEventSubscribed)
            {
                _onHintsChanged.AddListener(RefreshHintsCount);
                _hintsEventSubscribed = true;
            }
        }

        void RefreshHintsCount(int count)
        {
            if (_hintsAmountText) _hintsAmountText.text = count.ToString();
            // Interactable state is driven from Update() so the button reflects
            // not just inventory but also "no unshown hints left" — auto-hints
            // that consume the last available hint don't fire OnHintsChanged.
        }

        void OnHintClicked()
        {
            _hintSystem?.UseHint();
        }

        void Update()
        {
            if (_levelController == null) return;

            // Once the player confirms, the level walks AwaitInput → ValidateAnswer
            // → (delay) → CalculateResult/ShowResult. Lock every action button
            // during that window so a stray tap can't fire while the curve
            // animation is still playing or the result screen is mid-fade.
            bool awaitingInput = _levelController.CurrentState == LevelState.AwaitInput;

            if (_undoButton)
                _undoButton.interactable = awaitingInput
                                           && _levelController.ActionHistory != null
                                           && _levelController.ActionHistory.CanUndo;

            if (_resetButton)
                _resetButton.interactable = awaitingInput;

            if (_confirmButton)
                _confirmButton.interactable = awaitingInput
                                              && _levelController.AnswerSystem != null
                                              && _levelController.AnswerSystem.HasSelection
                                              && _levelController.AnswerSystem.IsActive;

            if (_hintButton && _hintSystem != null)
                _hintButton.interactable = awaitingInput
                                           && _hintSystem.PaidHintCount > 0
                                           && _hintSystem.HasUnshownHints;
        }

        void OnConfirmClicked()
        {
            _levelController.AnswerSystem?.ConfirmAnswer();
        }

        void OnUndoClicked()
        {
            _levelController.UndoLastAction();
            TrackLevelEvent(AnalyticsEventNames.ActionUndo, includeSector: false,
                extra: new Dictionary<string, object> { ["actionType"] = "answer_select" });
        }

        void OnResetClicked()
        {
            _levelController.RestartLevel();
            TrackLevelEvent(AnalyticsEventNames.LevelReset, includeSector: true);
        }

        void TrackLevelEvent(string name, bool includeSector,
            Dictionary<string, object> extra = null)
        {
            if (!ServiceLocator.Contains<IAnalyticsService>()) return;
            var levelData = _levelController != null ? _levelController.LevelData : null;
            if (levelData == null) return;

            var p = extra ?? new Dictionary<string, object>();
            p["levelId"] = levelData.LevelId;
            if (includeSector) p["sectorId"] = ExtractSectorId(levelData.LevelId);

            ServiceLocator.Get<IAnalyticsService>().TrackEvent(name, p);
        }

        static string ExtractSectorId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return string.Empty;
            int idx = levelId.IndexOf("_level_", System.StringComparison.Ordinal);
            return idx > 0 ? levelId.Substring(0, idx) : levelId;
        }

        void OnPauseClicked()
        {
            if (_levelController.Timer == null) return;

            _isPaused = !_isPaused;

            if (_isPaused)
                _levelController.Timer.Pause();
            else
                _levelController.Timer.Resume();
        }

        void OnDestroy()
        {
            if (_confirmButton) _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (_undoButton) _undoButton.onClick.RemoveListener(OnUndoClicked);
            if (_resetButton) _resetButton.onClick.RemoveListener(OnResetClicked);
            if (_pauseButton) _pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (_hintButton) _hintButton.onClick.RemoveListener(OnHintClicked);
            if (_onHintsChanged && _hintsEventSubscribed)
            {
                _onHintsChanged.RemoveListener(RefreshHintsCount);
                _hintsEventSubscribed = false;
            }
        }
    }
}
