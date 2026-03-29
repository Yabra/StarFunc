using System.Collections;
using UnityEngine;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class GhostAnimator : MonoBehaviour
    {
        [SerializeField] GhostVisuals _visuals;

        [Header("Idle Bob")]
        [SerializeField] float _bobAmplitude = 0.15f;
        [SerializeField] float _bobFrequency = 1.5f;

        [Header("Emotion Reactions")]
        [SerializeField] float _reactionDuration = 0.25f;
        [SerializeField] float _happyScalePeak = 1.2f;
        [SerializeField] float _sadScaleMin = 0.85f;
        [SerializeField] float _excitedScalePeak = 1.35f;

        float _baseY;
        Coroutine _reactionCoroutine;

        void Awake()
        {
            _baseY = transform.localPosition.y;
        }

        void Update()
        {
            float bobOffset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
            var pos = transform.localPosition;
            pos.y = _baseY + bobOffset;
            transform.localPosition = pos;
        }

        public void PlayEmotionReaction(GhostEmotion emotion)
        {
            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine = emotion switch
            {
                GhostEmotion.Happy => StartCoroutine(ScalePulseRoutine(_happyScalePeak)),
                GhostEmotion.Sad => StartCoroutine(ScalePulseRoutine(_sadScaleMin)),
                GhostEmotion.Excited => StartCoroutine(ScalePulseRoutine(_excitedScalePeak)),
                _ => null
            };
        }

        IEnumerator ScalePulseRoutine(float targetScale)
        {
            float elapsed = 0f;
            while (elapsed < _reactionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _reactionDuration);
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, targetScale, t * 2f)
                    : Mathf.Lerp(targetScale, 1f, (t - 0.5f) * 2f);
                _visuals.SetScale(scale);
                yield return null;
            }
            _visuals.SetScale(1f);
            _reactionCoroutine = null;
        }
    }
}
