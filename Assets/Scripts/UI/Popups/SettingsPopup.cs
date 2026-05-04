using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Settings popup with music/SFX volume sliders and a haptics toggle.
    /// Values are written through to <see cref="IAudioService"/> /
    /// <see cref="IFeedbackService"/>, which persist them to PlayerPrefs.
    /// </summary>
    public class SettingsPopup : UIPopup
    {
        [Header("Music")]
        [SerializeField] Slider _musicSlider;
        [SerializeField] TMP_Text _musicValueLabel;

        [Header("SFX")]
        [SerializeField] Slider _sfxSlider;
        [SerializeField] TMP_Text _sfxValueLabel;

        [Header("Haptics")]
        [SerializeField] Toggle _hapticsToggle;

        [Header("Buttons")]
        [SerializeField] Button _closeButton;

        IAudioService _audio;
        IFeedbackService _feedback;
        bool _suppressEvents;

        void Awake()
        {
            if (_musicSlider) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (_sfxSlider) _sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            if (_hapticsToggle) _hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_musicSlider) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (_sfxSlider) _sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            if (_hapticsToggle) _hapticsToggle.onValueChanged.RemoveListener(OnHapticsChanged);
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            ResolveServices();
            SyncFromServices();
        }

        public void Show() => Show((PopupData)null);

        void ResolveServices()
        {
            if (_audio == null && ServiceLocator.Contains<IAudioService>())
                _audio = ServiceLocator.Get<IAudioService>();
            if (_feedback == null && ServiceLocator.Contains<IFeedbackService>())
                _feedback = ServiceLocator.Get<IFeedbackService>();
        }

        void SyncFromServices()
        {
            _suppressEvents = true;

            if (_musicSlider && _audio != null)
            {
                _musicSlider.SetValueWithoutNotify(_audio.MusicVolume);
                UpdateMusicLabel(_audio.MusicVolume);
            }

            if (_sfxSlider && _audio != null)
            {
                _sfxSlider.SetValueWithoutNotify(_audio.SFXVolume);
                UpdateSFXLabel(_audio.SFXVolume);
            }

            if (_hapticsToggle && _feedback != null)
                _hapticsToggle.SetIsOnWithoutNotify(_feedback.HapticsEnabled);

            _suppressEvents = false;
        }

        void OnMusicChanged(float value)
        {
            if (_suppressEvents) return;
            _audio?.SetMusicVolume(value);
            UpdateMusicLabel(value);
        }

        void OnSFXChanged(float value)
        {
            if (_suppressEvents) return;
            _audio?.SetSFXVolume(value);
            UpdateSFXLabel(value);
        }

        void OnHapticsChanged(bool isOn)
        {
            if (_suppressEvents) return;
            _feedback?.SetHapticsEnabled(isOn);
        }

        void UpdateMusicLabel(float value)
        {
            if (_musicValueLabel) _musicValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        void UpdateSFXLabel(float value)
        {
            if (_sfxValueLabel) _sfxValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
