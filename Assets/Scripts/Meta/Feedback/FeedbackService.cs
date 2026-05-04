using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    public class FeedbackService : IFeedbackService
    {
        const string HapticsKey = "HapticsEnabled";

        readonly IAudioService _audio;
        readonly AudioConfig _audioConfig;
        readonly VfxConfig _vfxConfig;

        bool _hapticsEnabled;

        public bool HapticsEnabled => _hapticsEnabled;

        public FeedbackService(IAudioService audio = null, AudioConfig audioConfig = null,
            VfxConfig vfxConfig = null)
        {
            _audio = audio;
            _audioConfig = audioConfig;
            _vfxConfig = vfxConfig;
            _hapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        }

        public void PlayFeedback(FeedbackType type) => PlayInternal(type, hasPosition: false, default);

        public void PlayFeedback(FeedbackType type, Vector3 worldPosition) =>
            PlayInternal(type, hasPosition: true, worldPosition);

        void PlayInternal(FeedbackType type, bool hasPosition, Vector3 worldPosition)
        {
            if (_audio != null && _audioConfig != null)
            {
                var clip = _audioConfig.GetFeedbackClip(type);
                if (clip != null) _audio.PlaySFX(clip);
            }

            if (_hapticsEnabled && ShouldVibrate(type))
                Handheld.Vibrate();

            if (_vfxConfig == null) return;
            var prefab = _vfxConfig.GetVfxPrefab(type);
            if (prefab == null) return;

            // Without an explicit world position, spawn at origin — useful for
            // UI-triggered events (sector unlock, level complete) where the
            // effect's own animation and screen-space placement do the work.
            var pos = hasPosition ? worldPosition : Vector3.zero;
            Object.Instantiate(prefab, pos, Quaternion.identity);
        }

        public void SetHapticsEnabled(bool enabled)
        {
            _hapticsEnabled = enabled;
            PlayerPrefs.SetInt(HapticsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        static bool ShouldVibrate(FeedbackType type)
        {
            return type == FeedbackType.StarError;
        }
    }
}
