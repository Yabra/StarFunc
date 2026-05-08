using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Endless-mode picker. Lets the player choose difficulty, task type, and
    /// an optional seed before rolling a level via
    /// <see cref="EndlessLevelGenerator"/>. Mirrors the SettingsPopup shape:
    /// designer wires inspector refs, popup syncs from defaults on Show, and
    /// applies on user input. Play also handles the Hub UI hide/restore so
    /// HubScreen's GraphicRaycaster doesn't intercept clicks while the level
    /// scene runs additively on top of Hub.
    /// </summary>
    public class EndlessPopup : UIPopup
    {
        [Header("Difficulty")]
        [SerializeField] Toggle _difficultyEasyToggle;
        [SerializeField] Toggle _difficultyMediumToggle;
        [SerializeField] Toggle _difficultyHardToggle;

        [Header("Task Type")]
        [SerializeField] Toggle _taskChooseFunctionToggle;
        [SerializeField] Toggle _taskChooseCoordinateToggle;

        [Header("Seed")]
        [Tooltip("Optional seed input. Leave empty to roll a fresh random " +
                 "level on each Play.")]
        [SerializeField] TMP_InputField _seedField;

        [Header("Buttons")]
        [SerializeField] Button _playButton;
        [SerializeField] Button _closeButton;

        [Header("Refs")]
        [SerializeField] UIService _uiService;

        [Header("Defaults")]
        [SerializeField] EndlessDifficulty _defaultDifficulty = EndlessDifficulty.Easy;
        [SerializeField] TaskType _defaultTaskType = TaskType.ChooseFunction;

        SceneFlowManager _sceneFlow;
        ILivesService _lives;
        EndlessLevelGenerator _generator;
        HubScreen _hubScreenSnapshot;

        void Awake()
        {
            _generator = new EndlessLevelGenerator();

            if (_playButton) _playButton.onClick.AddListener(OnPlayClicked);
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_playButton) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
            // Drop any in-flight unload subscription if the popup itself is
            // torn down (Hub scene reload, etc.) before Level returns.
            SceneManager.sceneUnloaded -= OnLevelUnloaded;
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            ResolveServices();
            SyncFromDefaults();
        }

        public void Show() => Show((PopupData)null);

        void ResolveServices()
        {
            if (_sceneFlow == null && ServiceLocator.Contains<SceneFlowManager>())
                _sceneFlow = ServiceLocator.Get<SceneFlowManager>();
            if (_lives == null && ServiceLocator.Contains<ILivesService>())
                _lives = ServiceLocator.Get<ILivesService>();
        }

        void SyncFromDefaults()
        {
            // Reset to designer-configured defaults each time the popup
            // opens — players don't expect last-session's settings to stick
            // before any seed-share UX exists.
            SetDifficultyToggle(_defaultDifficulty);
            SetTaskTypeToggle(_defaultTaskType);

            if (_seedField) _seedField.SetTextWithoutNotify(string.Empty);
        }

        void SetDifficultyToggle(EndlessDifficulty d)
        {
            if (_difficultyEasyToggle)
                _difficultyEasyToggle.SetIsOnWithoutNotify(d == EndlessDifficulty.Easy);
            if (_difficultyMediumToggle)
                _difficultyMediumToggle.SetIsOnWithoutNotify(d == EndlessDifficulty.Medium);
            if (_difficultyHardToggle)
                _difficultyHardToggle.SetIsOnWithoutNotify(d == EndlessDifficulty.Hard);
        }

        void SetTaskTypeToggle(TaskType t)
        {
            if (_taskChooseFunctionToggle)
                _taskChooseFunctionToggle.SetIsOnWithoutNotify(t == TaskType.ChooseFunction);
            if (_taskChooseCoordinateToggle)
                _taskChooseCoordinateToggle.SetIsOnWithoutNotify(t == TaskType.ChooseCoordinate);
        }

        EndlessDifficulty ReadDifficulty()
        {
            if (_difficultyHardToggle && _difficultyHardToggle.isOn) return EndlessDifficulty.Hard;
            if (_difficultyMediumToggle && _difficultyMediumToggle.isOn) return EndlessDifficulty.Medium;
            return EndlessDifficulty.Easy;
        }

        TaskType ReadTaskType()
        {
            if (_taskChooseCoordinateToggle && _taskChooseCoordinateToggle.isOn)
                return TaskType.ChooseCoordinate;
            return TaskType.ChooseFunction;
        }

        int? ReadSeed()
        {
            if (_seedField == null) return null;
            string raw = _seedField.text;
            if (string.IsNullOrWhiteSpace(raw)) return null;
            // Empty / non-numeric input falls back to a fresh random seed —
            // never block Play because the seed field had a typo.
            return int.TryParse(raw.Trim(), out var s) ? s : (int?)null;
        }

        void OnPlayClicked()
        {
            ResolveServices();

            if (_sceneFlow == null)
            {
                Debug.LogWarning("[EndlessPopup] SceneFlowManager not registered.");
                return;
            }

            // Mirror LevelLauncher: empty inventory pops the No-Lives popup
            // and never starts a scene load.
            if (_lives != null && !_lives.HasLives())
            {
                Debug.Log("[EndlessPopup] Endless launch blocked — no lives.");
                Hide();
                if (_uiService != null) _uiService.ShowPopup<NoLivesPopup>(null);
                return;
            }

            var level = _generator.Generate(new EndlessOptions
            {
                Difficulty = ReadDifficulty(),
                TaskType = ReadTaskType(),
                Seed = ReadSeed()
            });

            HideHubScreenWhileLevelRuns();
            Hide();
            _sceneFlow.LoadLevel(level);
        }

        // =========================================================================
        // Hub UI hide/restore — same pattern as the original EndlessHubButton.
        // Normal level entry deactivates HubScreen as a side-effect of pushing
        // SectorScreen onto the UIService stack; the endless flow skips that
        // hop, so HubScreen's raycaster keeps eating clicks during the level.
        // We manually toggle gameObject.SetActive without going through
        // UIService.HideScreen so the screen stack stays untouched.
        // =========================================================================

        void HideHubScreenWhileLevelRuns()
        {
            _hubScreenSnapshot = _uiService != null
                ? _uiService.GetScreen<HubScreen>()
                : null;

            if (_hubScreenSnapshot == null) return;

            _hubScreenSnapshot.Hide();
            SceneManager.sceneUnloaded += OnLevelUnloaded;
        }

        void OnLevelUnloaded(Scene scene)
        {
            if (scene.name != "Level") return;
            SceneManager.sceneUnloaded -= OnLevelUnloaded;

            if (_hubScreenSnapshot != null)
            {
                _hubScreenSnapshot.Show();
                _hubScreenSnapshot = null;
            }
        }
    }
}
