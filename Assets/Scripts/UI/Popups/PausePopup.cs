using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Gameplay;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Mid-level pause popup. Pauses both the meta <see cref="ITimerService"/>
    /// and the gameplay <see cref="LevelTimer"/> on Show, resumes them on
    /// "Continue", or stops them and returns to Hub on "Exit". The two
    /// timers are independent — TimerService is the meta-level wall-clock
    /// (analytics / cross-screen), LevelTimer is what the HUD reads.
    /// </summary>
    public class PausePopup : UIPopup
    {
        [Header("Buttons")]
        [SerializeField] Button _continueButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] Button _hubButton;

        ITimerService _timer;
        SceneFlowManager _sceneFlow;
        IUIService _ui;
        LevelController _levelController;

        void Awake()
        {
            if (_continueButton) _continueButton.onClick.AddListener(OnContinue);
            if (_settingsButton) _settingsButton.onClick.AddListener(OnSettings);
            if (_hubButton) _hubButton.onClick.AddListener(OnHub);
        }

        void OnDestroy()
        {
            if (_continueButton) _continueButton.onClick.RemoveListener(OnContinue);
            if (_settingsButton) _settingsButton.onClick.RemoveListener(OnSettings);
            if (_hubButton) _hubButton.onClick.RemoveListener(OnHub);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            ResolveServices();
            _timer?.PauseTimer();
            _levelController?.Timer?.Pause();
        }

        /// <summary>Show without PopupData — buttons drive everything.</summary>
        public void Show() => Show((PopupData)null);

        void ResolveServices()
        {
            if (_timer == null && ServiceLocator.Contains<ITimerService>())
                _timer = ServiceLocator.Get<ITimerService>();
            if (_sceneFlow == null && ServiceLocator.Contains<SceneFlowManager>())
                _sceneFlow = ServiceLocator.Get<SceneFlowManager>();
            if (_ui == null && ServiceLocator.Contains<IUIService>())
                _ui = ServiceLocator.Get<IUIService>();
            // LevelController isn't a registered service — it lives in the
            // current Level scene, so resolve it via Find. Cached after the
            // first hit so subsequent pauses are O(1).
            if (_levelController == null)
                _levelController = FindAnyObjectByType<LevelController>(FindObjectsInactive.Include);
        }

        void OnContinue()
        {
            _timer?.ResumeTimer();
            _levelController?.Timer?.Resume();
            Hide();
        }

        void OnSettings()
        {
            if (_ui == null) return;

            var settings = _ui.GetPopup<SettingsPopup>();
            if (settings == null)
            {
                Debug.LogWarning("[PausePopup] SettingsPopup not found.");
                return;
            }

            // Step out of the way while Settings is shown, then come back
            // once it closes. Subscribing once via a self-unsubscribing
            // local handler keeps the lifecycle clean — no leaked listeners
            // if the player opens Settings again later.
            void HandleSettingsClosed()
            {
                settings.OnHidden -= HandleSettingsClosed;
                Show();
            }

            settings.OnHidden += HandleSettingsClosed;
            Hide();
            _ui.ShowPopup<SettingsPopup>(null);
        }

        void OnHub()
        {
            _timer?.StopTimer();
            _levelController?.Timer?.Stop();
            Hide();
            _sceneFlow?.LoadScene("Hub");
        }
    }
}
