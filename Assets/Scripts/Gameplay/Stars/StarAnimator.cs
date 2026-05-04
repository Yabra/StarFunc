using System.Collections;
using DG.Tweening;
using StarFunc.Core;
using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Star animations driven by DOTween. Each routine returns a coroutine so
    /// callers can <c>yield return star.PlayPlace()</c>; internally the coroutine
    /// builds a DOTween Sequence, waits for it to finish, then snaps the visuals
    /// to a clean post-animation state.
    ///
    /// Only DOTween core (DOTween.dll) APIs are used here — DOTween.To() with
    /// lambdas — so this works without adding the loose Module .cs files to our
    /// asmdef. SpriteRenderer color is animated via getter/setter lambdas
    /// against <see cref="StarVisuals"/> helpers.
    /// </summary>
    public class StarAnimator : MonoBehaviour
    {
        [SerializeField] StarVisuals _visuals;

        [Header("Appear")]
        [SerializeField] float _appearDuration = 0.3f;

        [Header("Place")]
        [SerializeField] float _placeDuration = 0.35f;
        [SerializeField] float _placeScalePeak = 1.3f;
        [SerializeField] float _placeFlashDuration = 0.1f;
        [SerializeField] Color _placeFlashColor = Color.white;

        [Header("Error")]
        [SerializeField] float _errorDuration = 0.35f;
        [SerializeField] float _errorShakeAmount = 0.05f;
        [SerializeField] int _errorShakeCycles = 4;

        [Header("Restore")]
        [SerializeField] float _restoreDuration = 0.6f;
        [SerializeField] Color _restoreGoldColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] float _restoreScalePeak = 1.4f;

        Tween _activeTween;

        public Coroutine PlayAppear() => StartCoroutine(AppearRoutine());
        public Coroutine PlayPlace() => StartCoroutine(PlaceRoutine());
        public Coroutine PlayError() => StartCoroutine(ErrorRoutine());
        public Coroutine PlayRestore() => StartCoroutine(RestoreRoutine());

        IEnumerator AppearRoutine()
        {
            KillActive();
            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => 0f, x => _visuals.SetScale(x), 1f, _appearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(DOTween.To(() => 0f, x => _visuals.SetAlpha(x), 1f, _appearDuration)
                .SetEase(Ease.OutQuad));
            _activeTween = seq;
            yield return WaitFor(seq);

            _visuals.SetScale(1f);
            _visuals.SetAlpha(1f);
        }

        IEnumerator PlaceRoutine()
        {
            KillActive();
            Color preColor = _visuals.GetColor();
            _visuals.SetMainColor(_placeFlashColor);

            float halfDuration = _placeDuration * 0.5f;

            var seq = DOTween.Sequence();

            // Scale ping-pong: 1 → peak → 1
            seq.Append(DOTween.To(() => 1f, x => _visuals.SetScale(x), _placeScalePeak, halfDuration)
                .SetEase(Ease.OutQuad));
            seq.Append(DOTween.To(() => _placeScalePeak, x => _visuals.SetScale(x), 1f, halfDuration)
                .SetEase(Ease.InQuad));

            // Color: hold the flash for _placeFlashDuration, then ease back to preColor.
            seq.Insert(_placeFlashDuration,
                DOTween.To(() => _placeFlashColor, c => _visuals.SetMainColor(c),
                           preColor, _placeDuration - _placeFlashDuration));

            // Glow pulse: rise to peak alongside scale, then fall back.
            seq.Insert(0f,
                DOTween.To(() => 0.4f, x => _visuals.SetGlowAlpha(x), 1f, halfDuration));
            seq.Insert(halfDuration,
                DOTween.To(() => 1f, x => _visuals.SetGlowAlpha(x), 0.4f, halfDuration));

            _activeTween = seq;
            yield return WaitFor(seq);

            _visuals.SetScale(1f);
            _visuals.SetMainColor(preColor);
            _visuals.SetGlowAlpha(0.4f);
        }

        IEnumerator ErrorRoutine()
        {
            KillActive();
            Color preColor = _visuals.GetColor();
            Vector3 originalPos = transform.localPosition;

            var seq = DOTween.Sequence();

            // Decaying horizontal shake — sine-driven, _errorShakeCycles full back-and-forths.
            // We tween a 0..1 parameter and write the position each frame from inside the setter.
            seq.Append(DOTween.To(() => 0f, t =>
            {
                float fade = 1f - t;
                float offset = Mathf.Sin(t * Mathf.PI * 2f * _errorShakeCycles)
                               * _errorShakeAmount * fade;
                transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
            }, 1f, _errorDuration));

            // Red flash that eases back to preColor over the same duration.
            seq.Insert(0f,
                DOTween.To(() => ColorTokens.ERROR, c => _visuals.SetMainColor(c),
                           preColor, _errorDuration));

            _activeTween = seq;
            yield return WaitFor(seq);

            transform.localPosition = originalPos;
            _visuals.SetMainColor(preColor);
        }

        IEnumerator RestoreRoutine()
        {
            KillActive();
            // Golden glow + scale pulse. The connecting line between this star and
            // its constellation neighbour is drawn by StarManager during the chain
            // animation — see StarManager.PlayConstellationRestore.
            Color preColor = _visuals.GetColor();
            _visuals.SetGlow(true);

            float halfDuration = _restoreDuration * 0.5f;

            var seq = DOTween.Sequence();

            // Color: ease into gold, then back to preColor.
            seq.Append(DOTween.To(() => preColor, c => _visuals.SetMainColor(c),
                                  _restoreGoldColor, halfDuration));
            seq.Append(DOTween.To(() => _restoreGoldColor, c => _visuals.SetMainColor(c),
                                  preColor, halfDuration));

            // Scale: gentle pulse, paralleled with the color fade.
            seq.Insert(0f,
                DOTween.To(() => 1f, x => _visuals.SetScale(x), _restoreScalePeak, halfDuration)
                    .SetEase(Ease.OutQuad));
            seq.Insert(halfDuration,
                DOTween.To(() => _restoreScalePeak, x => _visuals.SetScale(x), 1f, halfDuration)
                    .SetEase(Ease.InQuad));

            // Glow: rise to 1, then settle at 0.7.
            seq.Insert(0f,
                DOTween.To(() => 0.4f, x => _visuals.SetGlowAlpha(x), 1f, halfDuration));
            seq.Insert(halfDuration,
                DOTween.To(() => 1f, x => _visuals.SetGlowAlpha(x), 0.7f, halfDuration));

            _activeTween = seq;
            yield return WaitFor(seq);

            _visuals.SetScale(1f);
            _visuals.SetMainColor(preColor);
            _visuals.SetGlowAlpha(0.7f);
        }

        void KillActive()
        {
            if (_activeTween != null && _activeTween.IsActive())
                _activeTween.Kill();
            _activeTween = null;
        }

        void OnDisable() => KillActive();

        static IEnumerator WaitFor(Tween tween)
        {
            while (tween != null && tween.IsActive() && !tween.IsComplete())
                yield return null;
        }
    }
}
