using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Plain-class facade over <see cref="MusicPlayer"/> and <see cref="SFXPlayer"/>.
    /// Volumes are mirrored to PlayerPrefs so user settings survive restarts.
    /// </summary>
    public class AudioService : IAudioService
    {
        const string MusicVolumeKey = "Audio.MusicVolume";
        const string SFXVolumeKey = "Audio.SFXVolume";

        readonly MusicPlayer _music;
        readonly SFXPlayer _sfx;

        float _musicVolume;
        float _sfxVolume;

        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;

        public AudioService(MusicPlayer music, SFXPlayer sfx)
        {
            _music = music;
            _sfx = sfx;

            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SFXVolumeKey, 1f));

            _music?.SetVolume(_musicVolume);
            _sfx?.SetVolume(_sfxVolume);
        }

        public void PlayMusic(AudioClip clip, float crossfadeDuration = 0.5f)
        {
            _music?.CrossfadeTo(clip, crossfadeDuration);
        }

        public void StopMusic() => _music?.Stop();

        public void PlaySFX(AudioClip clip) => _sfx?.Play(clip);

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _music?.SetVolume(_musicVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            _sfx?.SetVolume(_sfxVolume);
            PlayerPrefs.SetFloat(SFXVolumeKey, _sfxVolume);
            PlayerPrefs.Save();
        }
    }
}
