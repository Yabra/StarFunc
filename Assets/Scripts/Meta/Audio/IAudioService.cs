using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Audio playback contract. Music has crossfade; SFX is one-shot through a pool.
    /// Volumes are persisted across sessions (PlayerPrefs).
    /// </summary>
    public interface IAudioService
    {
        float MusicVolume { get; }
        float SFXVolume { get; }

        /// <summary>Play (or crossfade to) a music clip. Pass <c>null</c> to stop.</summary>
        void PlayMusic(AudioClip clip, float crossfadeDuration = 0.5f);

        /// <summary>Stop any music playing. Equivalent to <c>PlayMusic(null)</c>.</summary>
        void StopMusic();

        /// <summary>One-shot SFX through the source pool. No-op when <paramref name="clip"/> is null.</summary>
        void PlaySFX(AudioClip clip);

        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
    }
}
