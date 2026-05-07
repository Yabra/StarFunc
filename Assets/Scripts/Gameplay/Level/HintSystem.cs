using System;
using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Two independent hint systems for a level (Architecture.md §5.7):
    ///
    /// 1. **Auto-hints (free)**: driven by <c>LevelData.Hints[]</c> + the
    ///    <c>ShowHints</c> flag, fired by triggers (OnLevelStart, AfterErrors,
    ///    OnFirstInteraction). Do NOT consume the player's hint inventory.
    ///
    /// 2. **Paid hints (consumable)**: stored in
    ///    <c>PlayerSaveData.Consumables["hints"]</c>. Activated by the HUD
    ///    HintButton via <see cref="UseHint"/>. Each press consumes one unit
    ///    and reveals the next un-shown level-configured hint. If inventory
    ///    is empty, fires <see cref="OnPaidHintEmpty"/> so a shop offer can
    ///    open (handled by HUD/UI layer).
    ///
    /// The two systems share the "already shown" registry — a hint surfaced
    /// by an auto-trigger won't be re-served as a paid hint, and vice versa.
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        const string ConsumableKey = "hints";

        [Header("References")]
        [SerializeField] LevelController _levelController;
        [SerializeField] HintPopup _hintPopup;
        [SerializeField] StarManager _starManager;
        [SerializeField] LevelDataEvent _onLevelStarted;
        [SerializeField] AnswerDataEvent _onAnswerSelected;
        [Tooltip("Raised when the player's paid-hint inventory changes. " +
                 "Hub topbar HintsDisplay listens to this for live count updates.")]
        [SerializeField] GameEvent<int> _onHintsChanged;

        [Header("Behaviour")]
        [SerializeField] float _autoHintTimeout = 5f;
        [Tooltip("Grace period during which an OnLevelStart hint can't be tap-dismissed, " +
                 "so reflex taps right after the level loads don't kill it before the player reads.")]
        [SerializeField] float _onLevelStartMinDisplay = 1.5f;

        ISaveService _saveService;
        PlayerSaveData _save;
        LevelData _levelData;
        AnswerSystem _answerSystem;

        bool _initialized;
        bool _firstInteractionFired;
        bool _answerSelectedFired;
        int _lastErrorCountSeen;
        bool _tutorialMode;
        readonly HashSet<int> _shownHintIndices = new();
        readonly Queue<int> _pendingMandatory = new();
        bool _hintHookSubscribed;
        bool _starHookSubscribed;
        bool _answerSelectedHookSubscribed;

        /// <summary>Fires when paid-hint inventory is empty on a UseHint() call. UI hooks this to show a shop offer.</summary>
        public event Action OnPaidHintEmpty;

        /// <summary>Fires when there are no more unseen hints to show (paid press is rejected).</summary>
        public event Action OnNoHintsAvailable;

        /// <summary>Current paid-hint balance (read-only). 0 if save not loaded yet.</summary>
        public int PaidHintCount =>
            _save != null && _save.Consumables != null
                && _save.Consumables.TryGetValue(ConsumableKey, out var n) ? n : 0;

        /// <summary>
        /// True iff the current level still has at least one configured hint
        /// that hasn't been shown yet (auto OR paid). When false, a paid press
        /// would just raise <see cref="OnNoHintsAvailable"/> — UI uses this to
        /// disable the hint button preemptively.
        /// </summary>
        public bool HasUnshownHints => FindNextUnshownHint() >= 0;

        void Start()
        {
            if (_levelController == null)
            {
                Debug.LogError("[HintSystem] LevelController reference is missing.");
                return;
            }

            if (ServiceLocator.Contains<ISaveService>())
            {
                _saveService = ServiceLocator.Get<ISaveService>();
                _save = _saveService.Load();
            }
            else
            {
                Debug.LogWarning("[HintSystem] ISaveService not registered; paid hints disabled.");
            }

            // Subscribe for future raises (e.g. RestartLevel re-runs Initialize).
            if (_onLevelStarted)
                _onLevelStarted.AddListener(InitializeForLevel);

            // Race recovery: LevelController shares this GameObject and its
            // Start() runs before ours, so by the time we get here the event
            // may have already fired with no listeners. If the controller is
            // already initialized, run our setup directly.
            if (!_initialized && _levelController.LevelData != null)
                InitializeForLevel(_levelController.LevelData);
        }

        void InitializeForLevel(LevelData data)
        {
            _levelData = data;
            _answerSystem = _levelController.AnswerSystem;
            _firstInteractionFired = false;
            _answerSelectedFired = false;
            _lastErrorCountSeen = 0;
            _tutorialMode = data != null && data.Type == LevelType.Tutorial;
            _shownHintIndices.Clear();
            _pendingMandatory.Clear();
            _initialized = true;

            if (_starManager != null && !_starHookSubscribed)
            {
                _starManager.OnStarTapped += HandleStarTapped;
                _starHookSubscribed = true;
            }

            if (_onAnswerSelected != null && !_answerSelectedHookSubscribed)
            {
                _onAnswerSelected.AddListener(HandleAnswerSelected);
                _answerSelectedHookSubscribed = true;
            }

            if (_hintPopup != null && !_hintHookSubscribed)
            {
                _hintPopup.OnDismissed += HandleHintDismissed;
                _hintHookSubscribed = true;
            }

            if (_levelData != null && _levelData.ShowHints)
                TryFireAutoHint(HintTrigger.OnLevelStart, 0);
        }

        void Update()
        {
            if (!_initialized || _levelData == null || !_levelData.ShowHints) return;

            int errors = _levelController.ErrorCount;
            if (errors != _lastErrorCountSeen)
            {
                _lastErrorCountSeen = errors;
                if (errors > 0) TryFireAutoHint(HintTrigger.AfterErrors, errors);
            }
        }

        void HandleStarTapped(StarEntity star)
        {
            if (_firstInteractionFired) return;
            _firstInteractionFired = true;
            if (_levelData != null && _levelData.ShowHints)
                TryFireAutoHint(HintTrigger.OnFirstInteraction, 0);
        }

        void HandleAnswerSelected(AnswerData data)
        {
            if (_answerSelectedFired) return;
            _answerSelectedFired = true;
            if (_levelData != null && _levelData.ShowHints)
                TryFireAutoHint(HintTrigger.OnAnswerSelected, 0);
        }

        void TryFireAutoHint(HintTrigger trigger, int currentErrors)
        {
            // Tutorial levels: queue every matching hint for the trigger and
            // play them sequentially via OnDismissed. Each one blocks the
            // player until tapped (mandatory).
            if (_tutorialMode)
            {
                bool added = false;
                while (true)
                {
                    int idx = FindUnshownHintForTrigger(trigger, currentErrors);
                    if (idx < 0) break;
                    _shownHintIndices.Add(idx); // reserve so the same hint isn't queued twice
                    _pendingMandatory.Enqueue(idx);
                    added = true;
                }

                if (added && (_hintPopup == null || !_hintPopup.IsVisible))
                    PumpMandatoryQueue();
                return;
            }

            int autoIdx = FindUnshownHintForTrigger(trigger, currentErrors);
            if (autoIdx < 0) return;
            ShowHint(_levelData.Hints[autoIdx], autoIdx, paid: false, mandatory: false);
        }

        void PumpMandatoryQueue()
        {
            if (_pendingMandatory.Count == 0) return;
            int idx = _pendingMandatory.Dequeue();
            // _shownHintIndices was already updated when queued; show without re-adding
            ShowHintInternal(_levelData.Hints[idx], idx, paid: false, mandatory: true);
        }

        void HandleHintDismissed()
        {
            if (_pendingMandatory.Count > 0)
                PumpMandatoryQueue();
        }

        int FindUnshownHintForTrigger(HintTrigger trigger, int currentErrors)
        {
            if (_levelData?.Hints == null) return -1;

            for (int i = 0; i < _levelData.Hints.Length; i++)
            {
                if (_shownHintIndices.Contains(i)) continue;
                var hint = _levelData.Hints[i];
                if (hint.Trigger != trigger) continue;
                if (trigger == HintTrigger.AfterErrors
                    && currentErrors < hint.TriggerAfterErrors) continue;
                return i;
            }

            return -1;
        }

        int FindNextUnshownHint()
        {
            if (_levelData?.Hints == null) return -1;
            for (int i = 0; i < _levelData.Hints.Length; i++)
                if (!_shownHintIndices.Contains(i)) return i;
            return -1;
        }

        /// <summary>
        /// Player-initiated paid hint. Consumes one hint from
        /// <c>Consumables["hints"]</c> and shows the next un-shown level hint.
        /// If inventory is 0, fires <see cref="OnPaidHintEmpty"/> and does
        /// nothing else (UI shows the buy offer). If no more hints to show,
        /// fires <see cref="OnNoHintsAvailable"/> without consuming.
        /// </summary>
        public void UseHint()
        {
            if (!_initialized) return;

            int hintIndex = FindNextUnshownHint();
            if (hintIndex < 0)
            {
                Debug.Log("[HintSystem] No unshown hints remaining for this level.");
                OnNoHintsAvailable?.Invoke();
                return;
            }

            int balance = PaidHintCount;
            if (balance <= 0)
            {
                Debug.Log("[HintSystem] Paid-hint inventory empty; raising OnPaidHintEmpty.");
                OnPaidHintEmpty?.Invoke();
                return;
            }

            // Consume one and persist.
            int newBalance = balance - 1;
            _save.Consumables[ConsumableKey] = newBalance;
            _save.IncrementVersion();
            _saveService?.Save(_save);

            // Notify the topbar inventory widget (lives in Hub scene) so the
            // count ticks down immediately rather than only refreshing on the
            // next Hub return.
            if (_onHintsChanged) _onHintsChanged.Raise(newBalance);

            ShowHint(_levelData.Hints[hintIndex], hintIndex, paid: true, mandatory: false);

            if (ServiceLocator.Contains<IAnalyticsService>())
            {
                ServiceLocator.Get<IAnalyticsService>().TrackEvent(
                    AnalyticsEventNames.HintUsed,
                    new Dictionary<string, object>
                    {
                        ["levelId"] = _levelData.LevelId,
                        ["hintIndex"] = hintIndex
                    });
            }
        }

        void ShowHint(HintConfig hint, int index, bool paid, bool mandatory)
        {
            _shownHintIndices.Add(index);
            ShowHintInternal(hint, index, paid, mandatory);
        }

        void ShowHintInternal(HintConfig hint, int index, bool paid, bool mandatory)
        {
            if (_hintPopup == null)
            {
                Debug.LogWarning($"[HintSystem] HintPopup not assigned; hint #{index} text='{hint.HintText}' (paid={paid}) not shown.");
                return;
            }

            float timeout = (paid || mandatory) ? -1f : _autoHintTimeout;
            float minDisplay = hint.Trigger == HintTrigger.OnLevelStart
                ? _onLevelStartMinDisplay
                : 0f;
            _hintPopup.Show(hint.HintText, hint.HighlightPosition, timeout, mandatory,
                minDisplay, hint.HighlightSize);
            Debug.Log($"[HintSystem] Hint #{index} shown ({(paid ? "paid" : hint.Trigger.ToString())}{(mandatory ? ", mandatory" : "")}): \"{hint.HintText}\"");
        }

        void OnDestroy()
        {
            if (_starManager != null && _starHookSubscribed)
            {
                _starManager.OnStarTapped -= HandleStarTapped;
                _starHookSubscribed = false;
            }
            if (_onAnswerSelected != null && _answerSelectedHookSubscribed)
            {
                _onAnswerSelected.RemoveListener(HandleAnswerSelected);
                _answerSelectedHookSubscribed = false;
            }
            if (_onLevelStarted)
                _onLevelStarted.RemoveListener(InitializeForLevel);
            if (_hintPopup != null && _hintHookSubscribed)
            {
                _hintPopup.OnDismissed -= HandleHintDismissed;
                _hintHookSubscribed = false;
            }
        }
    }
}
