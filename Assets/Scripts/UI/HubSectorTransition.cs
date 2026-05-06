using System;
using System.Collections.Generic;
using DG.Tweening;
using StarFunc.Core;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// Animates a Hub↔Sector screen swap as a focused zoom. The "pivoted"
    /// rect (HubScreen on ZoomIn, also HubScreen on ZoomOut) scales around
    /// the tapped sector node so the node stays planted in screen space
    /// while everything else fans out from it. The other rect cross-fades
    /// in/out at a slight overscale for an "emerging from depth" feel.
    /// <para>
    /// We don't manipulate <c>pivot</c> — on stretched RectTransforms the
    /// anchored-position compensation isn't reliable and causes the canvas
    /// to visibly snap before the tween starts. Instead we leave the pivot
    /// at its scene-time value and offset <c>transform.position</c> each
    /// frame so the focus point stays planted as the rect scales.
    /// </para>
    /// </summary>
    public class HubSectorTransition : MonoBehaviour, IHubSectorTransition
    {
        [Tooltip("Total tween duration for both ZoomIn and ZoomOut (seconds).")]
        [SerializeField] float _duration = 0.7f;

        [Tooltip("Final scale of the source screen on ZoomIn (it grows past " +
                 "the camera). Same value is reused as the dest's start scale " +
                 "for ZoomOut so the entry and exit feel symmetric.")]
        [SerializeField] float _sourceEndScale = 2f;

        [Tooltip("Initial scale of the destination screen on ZoomIn (it " +
                 "settles down to 1). > 1 sells 'emerging from depth'; 1 " +
                 "makes it a pure cross-fade.")]
        [SerializeField] float _destStartScale = 1.4f;

        [SerializeField] Ease _ease = Ease.InOutQuad;

        Sequence _activeSequence;
        RectTransform _lastFocusNode;

        // Cached "true rest" position/scale per RectTransform so a
        // mid-flight transition that gets killed by a fresh one doesn't
        // poison the new transition with a bogus origin. Captured only
        // once per rect — the first time we encounter it (when the rect
        // is presumed to be at rest from scene-time authoring).
        readonly Dictionary<RectTransform, Vector3> _restPos = new();
        readonly Dictionary<RectTransform, Vector3> _restScale = new();

        void Awake()
        {
            ServiceLocator.Register<IHubSectorTransition>(this);
        }

        void OnDestroy()
        {
            if (_activeSequence != null && _activeSequence.IsActive())
                _activeSequence.Kill();
            ServiceLocator.Unregister<IHubSectorTransition>(this);
        }

        public void ZoomIn(UIScreen source, UIScreen dest, RectTransform focusNode,
            Action onComplete)
        {
            _lastFocusNode = focusNode;
            RunTransition(source, dest, focusNode, isZoomIn: true, onComplete);
        }

        public void ZoomOut(UIScreen source, UIScreen dest, RectTransform focusNode,
            Action onComplete)
        {
            var node = focusNode != null ? focusNode : _lastFocusNode;
            RunTransition(source, dest, node, isZoomIn: false, onComplete);
        }

        void RunTransition(UIScreen source, UIScreen dest, RectTransform focusNode,
            bool isZoomIn, Action onComplete)
        {
            if (source == null || dest == null)
            {
                onComplete?.Invoke();
                return;
            }

            var sourceRect = source.transform as RectTransform;
            var destRect = dest.transform as RectTransform;
            var sourceCG = source.GetComponent<CanvasGroup>();
            var destCG = dest.GetComponent<CanvasGroup>();

            // Capture each rect's true rest BEFORE killing any prior
            // sequence — so the first transition records the genuine
            // resting position. Subsequent calls reuse the cache.
            EnsureRestCached(sourceRect);
            EnsureRestCached(destRect);

            // A previous sequence may have been killed mid-flight; force
            // the rects back to their true rest pose first so the new
            // transition starts from a clean baseline (otherwise any
            // residual offset would be visible as a "jump" on frame 1).
            if (_activeSequence != null && _activeSequence.IsActive())
            {
                _activeSequence.Kill();
                ResetToRest(sourceRect);
                ResetToRest(destRect);
            }

            // Both screens need to be live during the cross-fade; alpha is
            // what hides them, not deactivation.
            source.gameObject.SetActive(true);
            dest.gameObject.SetActive(true);

            // ZoomIn: HubScreen (source) grows around the focus node.
            // ZoomOut: HubScreen (dest) re-emerges around the same node.
            // The "other" rect just cross-fades at a slight overscale.
            RectTransform pivotedRect = isZoomIn ? sourceRect : destRect;
            RectTransform crossfadeRect = isZoomIn ? destRect : sourceRect;
            float pivotedStart = isZoomIn ? 1f : _sourceEndScale;
            float pivotedEnd = isZoomIn ? _sourceEndScale : 1f;
            float crossfadeStart = isZoomIn ? _destStartScale : 1f;
            float crossfadeEnd = isZoomIn ? 1f : _destStartScale;

            // Use cached rest values, never the rect's current pose,
            // so a killed-mid-flight tween can't poison this run.
            Vector3 pivotedOrigPos = _restPos[pivotedRect];
            Vector3 crossfadeOrigPos = _restPos[crossfadeRect];

            Vector3 focusOffset = focusNode != null
                ? focusNode.position - pivotedOrigPos
                : Vector3.zero;

            // Initial state. The pivoted rect needs an immediate position
            // offset so its focus point is already where it should be at
            // scale=pivotedStart (otherwise frame-1 would visibly jump).
            pivotedRect.localScale = Vector3.one * pivotedStart;
            pivotedRect.position = pivotedOrigPos
                + (1f - pivotedStart) * focusOffset;

            crossfadeRect.localScale = Vector3.one * crossfadeStart;
            crossfadeRect.position = crossfadeOrigPos;

            if (sourceCG != null) sourceCG.alpha = 1f;
            if (destCG != null)
            {
                destCG.alpha = 0f;
                // Don't intercept clicks while invisible; ShowScreen at the
                // end of the tween puts blocksRaycasts back to true.
                destCG.blocksRaycasts = false;
            }

            var seq = DOTween.Sequence();

            seq.Join(pivotedRect.DOScale(pivotedEnd, _duration)
                .SetEase(_ease)
                .OnUpdate(() =>
                {
                    float s = pivotedRect.localScale.x;
                    pivotedRect.position = pivotedOrigPos
                        + (1f - s) * focusOffset;
                }));
            seq.Join(crossfadeRect.DOScale(crossfadeEnd, _duration)
                .SetEase(_ease));

            if (sourceCG != null)
                seq.Join(DOTween
                    .To(() => sourceCG.alpha, a => sourceCG.alpha = a, 0f, _duration)
                    .SetEase(_ease));
            if (destCG != null)
                seq.Join(DOTween
                    .To(() => destCG.alpha, a => destCG.alpha = a, 1f, _duration)
                    .SetEase(_ease));

            seq.SetUpdate(true);
            seq.OnComplete(() =>
            {
                // Snap both rects back to their cached resting pose. The
                // dest reaches this naturally at the end of its tween;
                // the source/about-to-be-hidden screen would still be
                // mid-zoom otherwise, leaving stale state for the next
                // transition.
                ResetToRest(pivotedRect);
                ResetToRest(crossfadeRect);
                _activeSequence = null;
                onComplete?.Invoke();
            });

            _activeSequence = seq;
        }

        void EnsureRestCached(RectTransform rect)
        {
            if (rect == null) return;
            if (!_restPos.ContainsKey(rect))
                _restPos[rect] = rect.position;
            if (!_restScale.ContainsKey(rect))
                _restScale[rect] = rect.localScale;
        }

        void ResetToRest(RectTransform rect)
        {
            if (rect == null) return;
            if (_restPos.TryGetValue(rect, out var pos)) rect.position = pos;
            if (_restScale.TryGetValue(rect, out var scale)) rect.localScale = scale;
        }
    }
}
