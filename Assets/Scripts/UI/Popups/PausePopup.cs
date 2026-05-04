using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Mid-level pause popup. Pauses <see cref="ITimerService"/> on Show, resumes
    /// it on "Continue", or stops it and returns to Hub on "Exit".
    /// "Settings" is a stub until Task 4.x adds <c>SettingsPopup</c>.
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
        }

        void OnContinue()
        {
            _timer?.ResumeTimer();
            Hide();
        }

        void OnSettings()
        {
            _ui?.ShowPopup<SettingsPopup>(null);
        }

        void OnHub()
        {
            _timer?.StopTimer();
            Hide();
            _sceneFlow?.LoadScene("Hub");
        }
    }
}
