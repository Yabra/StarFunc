using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    public class FeedbackService : IFeedbackService
    {
        const string HapticsKey = "HapticsEnabled";

        bool _hapticsEnabled;

        public FeedbackService()
        {
            _hapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        }

        public void PlayFeedback(FeedbackType type)
        {
            // TODO: replace stub with real AudioService when implemented
            Debug.Log($"[FeedbackService] Audio: {type}");

            if (_hapticsEnabled && ShouldVibrate(type))
                Handheld.Vibrate();
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
