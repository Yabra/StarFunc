using System.Collections;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class StarAnimator : MonoBehaviour
    {
        [SerializeField] StarVisuals _visuals;

        [Header("Appear")]
        [SerializeField] float _appearDuration = 0.3f;

        [Header("Place")]
        [SerializeField] float _placeDuration = 0.2f;
        [SerializeField] float _placeScalePeak = 1.3f;

        [Header("Error")]
        [SerializeField] float _errorDuration = 0.3f;
        [SerializeField] float _errorShakeAmount = 0.1f;

        public Coroutine PlayAppear()
        {
            return StartCoroutine(AppearRoutine());
        }

        public Coroutine PlayPlace()
        {
            return StartCoroutine(PlaceRoutine());
        }

        public Coroutine PlayError()
        {
            return StartCoroutine(ErrorRoutine());
        }

        public Coroutine PlayRestore()
        {
            // Stub — will be implemented in Task 3.5
            return null;
        }

        IEnumerator AppearRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _appearDuration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad
                _visuals.SetScale(eased);
                _visuals.SetAlpha(eased);
                yield return null;
            }
            _visuals.SetScale(1f);
            _visuals.SetAlpha(1f);
        }

        IEnumerator PlaceRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _placeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _placeDuration);
                // Scale up then back down
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, _placeScalePeak, t * 2f)
                    : Mathf.Lerp(_placeScalePeak, 1f, (t - 0.5f) * 2f);
                _visuals.SetScale(scale);
                yield return null;
            }
            _visuals.SetScale(1f);
        }

        IEnumerator ErrorRoutine()
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            while (elapsed < _errorDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _errorDuration);
                float fade = 1f - t; // decay over time
                float offset = Mathf.Sin(t * Mathf.PI * 6f) * _errorShakeAmount * fade;
                transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            transform.localPosition = originalPos;
        }
    }
}
