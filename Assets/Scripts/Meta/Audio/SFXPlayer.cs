using System.Collections.Generic;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Pooled one-shot SFX player. Allocates <see cref="_poolSize"/> AudioSources
    /// up front and rotates through them; if every source is busy, the call is
    /// dropped (with a warning) rather than queuing.
    /// </summary>
    public class SFXPlayer : MonoBehaviour
    {
        [SerializeField, Range(4, 16)] int _poolSize = 10;

        readonly List<AudioSource> _pool = new();
        float _volume = 1f;
        int _nextIndex;

        void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.volume = _volume;
                _pool.Add(src);
            }
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            // Active sources keep their per-call volume; only newly-played ones pick this up.
        }

        public void Play(AudioClip clip)
        {
            if (clip == null) return;

            var src = GetFreeSource();
            if (src == null)
            {
                Debug.LogWarning("[SFXPlayer] Pool exhausted — SFX dropped: " + clip.name);
                return;
            }

            src.clip = clip;
            src.volume = _volume;
            src.Play();
        }

        AudioSource GetFreeSource()
        {
            // Linear scan starting from a rotating cursor — fair under burst.
            for (int i = 0; i < _pool.Count; i++)
            {
                int idx = (_nextIndex + i) % _pool.Count;
                if (!_pool[idx].isPlaying)
                {
                    _nextIndex = (idx + 1) % _pool.Count;
                    return _pool[idx];
                }
            }
            return null;
        }
    }
}
