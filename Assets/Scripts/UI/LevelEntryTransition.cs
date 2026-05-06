using System;
using DG.Tweening;
using StarFunc.Core;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// Hub→Level "dive in" transition. Scales a designated UI RectTransform
    /// up while fading the Hub CanvasGroup out, so the player feels the
    /// level path rushing past the camera into the tapped node.
    /// <para>
    /// We deliberately do NOT tween the camera or its orthographic size.
    /// The Hub canvas is in ScreenSpaceCamera mode and the shared
    /// <c>PersistentBackground</c> rescales itself to match the camera
    /// every frame, so a camera-ortho tween cancels out visually — both
    /// the canvas and the background compensate, and nothing appears to
    /// move. Animating the UI directly sidesteps both auto-rescalings.
    /// </para>
    /// </summary>
    public class LevelEntryTransition : MonoBehaviour, ILevelEntryTransition
    {
        [Tooltip("UI RectTransform that gets scaled up on ZoomIn and back on " +
                 "ZoomOut. Wire this to whatever should appear to grow toward " +
                 "the camera — typically the Screens parent or the SectorScreen " +
                 "RectTransform itself. Scaling is applied around the rect's " +
                 "current pivot.")]
        [SerializeField] RectTransform _uiZoomTarget;

        [Tooltip("CanvasGroup faded from 1→0 during ZoomIn (and reverse on " +
                 "ZoomOut) so the level path dissolves into the level scene " +
                 "instead of popping at the end of the scale tween.")]
        [SerializeField] CanvasGroup _hubCanvasGroup;

        [Tooltip("Total tween duration in seconds for both ZoomIn and ZoomOut.")]
        [SerializeField] float _duration = 1.1f;

        [Tooltip("Final localScale of _uiZoomTarget at the end of ZoomIn. " +
                 "2.5 means the UI ends ~2.5× its original size, which is the " +
                 "perceived 'rushing toward the camera' amount.")]
        [SerializeField] float _zoomScale = 2.5f;

        [Tooltip("Easing applied to scale and alpha tweens.")]
        [SerializeField] Ease _ease = Ease.InOutQuad;

        Vector3 _originalScale = Vector3.one;
        float _originalAlpha = 1f;
        bool _capturedOriginal;

        Tween _scaleTween;
        Tween _alphaTween;

        void Awake()
        {
            ServiceLocator.Register<ILevelEntryTransition>(this);
        }

        void OnDestroy()
        {
            KillTweens();
            ServiceLocator.Unregister<ILevelEntryTransition>(this);
        }

        public void ZoomIn(Vector3 worldPos, Action onComplete)
        {
            // worldPos is intentionally unused for now; the scale grows
            // around the rect's pivot. A future per-node variant could
            // shift _uiZoomTarget.pivot to align with the tapped node.
            _ = worldPos;

            CaptureOriginalOnce();
            KillTweens();

            bool drivesCallback = false;

            if (_uiZoomTarget != null)
            {
                drivesCallback = true;
                _scaleTween = _uiZoomTarget
                    .DOScale(_originalScale * _zoomScale, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            }

            if (_hubCanvasGroup != null)
            {
                // Cut interaction immediately so the (invisible mid-tween)
                // Hub UI doesn't eat Level-scene clicks. The Level scene
                // loads on top, but the Hub Canvas remains active in the
                // hierarchy — without flipping these flags its
                // GraphicRaycaster keeps swallowing pointer events.
                _hubCanvasGroup.interactable = false;
                _hubCanvasGroup.blocksRaycasts = false;

                var alphaTween = DOTween
                    .To(() => _hubCanvasGroup.alpha,
                        v => _hubCanvasGroup.alpha = v, 0f, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true);

                // If we don't have a scale target, the alpha tween drives the
                // callback so the SceneFlowManager still gets a "go" signal.
                if (!drivesCallback)
                    alphaTween.OnComplete(() => onComplete?.Invoke());

                _alphaTween = alphaTween;
            }

            if (_uiZoomTarget == null && _hubCanvasGroup == null)
                onComplete?.Invoke();
        }

        public void ZoomOut(Action onComplete)
        {
            if (!_capturedOriginal)
            {
                onComplete?.Invoke();
                return;
            }

            KillTweens();

            bool drivesCallback = false;

            if (_uiZoomTarget != null)
            {
                drivesCallback = true;
                _scaleTween = _uiZoomTarget
                    .DOScale(_originalScale, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            }

            if (_hubCanvasGroup != null)
            {
                var hubGroup = _hubCanvasGroup;
                bool driveCallbackHere = !drivesCallback;

                var alphaTween = DOTween
                    .To(() => hubGroup.alpha,
                        v => hubGroup.alpha = v, _originalAlpha, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true)
                    // Restore raycast/interaction flags at the end of the
                    // reverse tween — the Hub is back in focus and its
                    // buttons should react again. (Single OnComplete call
                    // because DOTween's OnComplete is a setter, not a
                    // chain — a later one would overwrite this.)
                    .OnComplete(() =>
                    {
                        hubGroup.interactable = true;
                        hubGroup.blocksRaycasts = true;
                        if (driveCallbackHere) onComplete?.Invoke();
                    });

                _alphaTween = alphaTween;
            }

            if (_uiZoomTarget == null && _hubCanvasGroup == null)
                onComplete?.Invoke();
        }

        void CaptureOriginalOnce()
        {
            if (_capturedOriginal) return;
            if (_uiZoomTarget != null)
                _originalScale = _uiZoomTarget.localScale;
            if (_hubCanvasGroup != null)
                _originalAlpha = _hubCanvasGroup.alpha;
            _capturedOriginal = true;
        }

        void KillTweens()
        {
            if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();
            if (_alphaTween != null && _alphaTween.IsActive()) _alphaTween.Kill();
            _scaleTween = null;
            _alphaTween = null;
        }
    }
}
