using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Designer-facing audio config: per-scene music tracks and per-FeedbackType
    /// SFX clips. Used by <c>BootInitializer</c>, <c>FeedbackService</c>, and
    /// any screen that wants to swap music (Hub/Level/etc).
    /// </summary>
    [CreateAssetMenu(menuName = "StarFunc/Config/AudioConfig", fileName = "AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Music")]
        public AudioClip BootMusic;
        public AudioClip HubMusic;
        public AudioClip LevelMusic;
        public float MusicCrossfadeDuration = 0.5f;

        [Header("Feedback SFX")]
        public AudioClip StarPlaced;
        public AudioClip StarError;
        public AudioClip LevelComplete;
        public AudioClip ConstellationRestored;
        public AudioClip ButtonTap;
        public AudioClip SectorUnlock;

        /// <summary>Clip for a feedback event, or <c>null</c> if not configured.</summary>
        public AudioClip GetFeedbackClip(FeedbackType type) => type switch
        {
            FeedbackType.StarPlaced => StarPlaced,
            FeedbackType.StarError => StarError,
            FeedbackType.LevelComplete => LevelComplete,
            FeedbackType.ConstellationRestored => ConstellationRestored,
            FeedbackType.ButtonTap => ButtonTap,
            FeedbackType.SectorUnlock => SectorUnlock,
            _ => null
        };
    }
}
