using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class StarRatingDisplay : MonoBehaviour
    {
        [SerializeField] Image[] _starImages;
        [SerializeField] Sprite _starFilled;
        [SerializeField] Sprite _starEmpty;
        [SerializeField] float _animationDelay = 0.3f;
        [SerializeField] float _punchScale = 1.3f;
        [SerializeField] float _punchDuration = 0.2f;

        int _pendingCount = -1;

        public void SetStars(int count, bool animate = false)
        {
            count = Mathf.Clamp(count, 0, _starImages.Length);

            if (animate)
            {
                // The result screen typically activates the GameObject
                // through a transition coroutine, so SetStars often runs
                // while we're still inactive. StartCoroutine throws in
                // that state — defer until OnEnable.
                if (!isActiveAndEnabled)
                {
                    _pendingCount = count;
                    ApplyStarsImmediate(0);
                    return;
                }

                StartCoroutine(AnimateStars(count));
                return;
            }

            _pendingCount = -1;
            ApplyStarsImmediate(count);
        }

        void OnEnable()
        {
            if (_pendingCount < 0) return;
            int c = _pendingCount;
            _pendingCount = -1;
            StartCoroutine(AnimateStars(c));
        }

        void ApplyStarsImmediate(int count)
        {
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].sprite = i < count ? _starFilled : _starEmpty;
                _starImages[i].transform.localScale = Vector3.one;
            }
        }

        IEnumerator AnimateStars(int count)
        {
            // Start with all empty
            ApplyStarsImmediate(0);

            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(_animationDelay);

                if (i >= _starImages.Length || _starImages[i] == null) continue;

                _starImages[i].sprite = _starFilled;
                yield return PunchScale(_starImages[i].transform);
            }
        }

        IEnumerator PunchScale(Transform target)
        {
            float elapsed = 0f;
            float half = _punchDuration * 0.5f;

            // Scale up
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.one * Mathf.Lerp(1f, _punchScale, t);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.one * Mathf.Lerp(_punchScale, 1f, t);
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}
