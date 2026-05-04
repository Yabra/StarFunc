using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Gameplay;
using StarFunc.Infrastructure;
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

        bool _isPaused;

        void Start()
        {
            if (_levelController == null)
            {
                Debug.LogError("[LevelHUD] LevelController reference is missing.");
                return;
            }

            InitializeWidgets();
            BindButtons();
        }

        void InitializeWidgets()
        {
            if (_timerDisplay)
                _timerDisplay.Initialize(_levelController);

            if (_livesDisplay)
                _livesDisplay.SetLives(5);

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
                {
                    _hintButton.onClick.AddListener(OnHintClicked);
                    _hintButton.interactable = true;
                }
                else
                {
                    _hintButton.interactable = false;
                }
            }
        }

        void OnHintClicked()
        {
            _hintSystem?.UseHint();
        }

        void Update()
        {
            if (_levelController == null) return;

            if (_undoButton)
                _undoButton.interactable = _levelController.ActionHistory != null
                                           && _levelController.ActionHistory.CanUndo;

            if (_confirmButton)
                _confirmButton.interactable = _levelController.AnswerSystem != null
                                              && _levelController.AnswerSystem.HasSelection
                                              && _levelController.AnswerSystem.IsActive;
        }

        void OnConfirmClicked()
        {
            _levelController.AnswerSystem?.ConfirmAnswer();
        }

        void OnUndoClicked()
        {
            _levelController.AnswerSystem?.ResetSelection();
            TrackLevelEvent(AnalyticsEventNames.ActionUndo, includeSector: false,
                extra: new Dictionary<string, object> { ["actionType"] = "answer_select" });
        }

        void OnResetClicked()
        {
            _levelController.ActionHistory?.Reset();
            _levelController.AnswerSystem?.ResetSelection();
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
        }
    }
}
