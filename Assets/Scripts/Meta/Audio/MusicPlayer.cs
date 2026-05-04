using DG.Tweening;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Two-source crossfading music player. Lives on a DontDestroyOnLoad
    /// GameObject created by <c>BootInitializer</c>; sources are auto-created
    /// in <see cref="Awake"/>.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource _sourceA;
        [SerializeField] AudioSource _sourceB;

        AudioSource _active;
        float _volume = 1f;
        Tween _fadeInTween;
        Tween _fadeOutTween;

        void Awake()
        {
            if (_sourceA == null) _sourceA = CreateSource();
            if (_sourceB == null) _sourceB = CreateSource();
            _active = _sourceA;
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            if (_active != null && _active.isPlaying)
                _active.volume = _volume;
        }

        /// <summary>
        /// Switch to <paramref name="clip"/> via volume crossfade. Same clip
        /// already playing is a no-op. <paramref name="duration"/> ≤ 0 swaps
        /// instantly. Pass <c>null</c> to fade out without a replacement.
        /// </summary>
        public void CrossfadeTo(AudioClip clip, float duration)
        {
            KillTweens();

            if (clip == null)
            {
                FadeOutAndStop(_active, duration);
                return;
            }

            if (_active != null && _active.clip == clip && _active.isPlaying) return;

            var next = _active == _sourceA ? _sourceB : _sourceA;
            var prev = _active;
            _active = next;

            next.clip = clip;
            next.volume = duration > 0f ? 0f : _volume;
            next.Play();

            if (duration > 0f)
            {
                _fadeInTween = DOTween
                    .To(() => next.volume, v => next.volume = v, _volume, duration)
                    .SetUpdate(true);
            }

            FadeOutAndStop(prev, duration);
        }

        public void Stop()
        {
            KillTweens();
            if (_sourceA) _sourceA.Stop();
            if (_sourceB) _sourceB.Stop();
        }

        void FadeOutAndStop(AudioSource src, float duration)
        {
            if (src == null || !src.isPlaying) return;
            if (duration <= 0f)
            {
                src.Stop();
                src.volume = 0f;
                return;
            }

            _fadeOutTween = DOTween
                .To(() => src.volume, v => src.volume = v, 0f, duration)
                .SetUpdate(true)
                .OnComplete(() => src.Stop());
        }

        void KillTweens()
        {
            if (_fadeInTween != null && _fadeInTween.IsActive()) _fadeInTween.Kill();
            if (_fadeOutTween != null && _fadeOutTween.IsActive()) _fadeOutTween.Kill();
            _fadeInTween = null;
            _fadeOutTween = null;
        }

        AudioSource CreateSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            return src;
        }

        void OnDestroy() => KillTweens();
    }
}
