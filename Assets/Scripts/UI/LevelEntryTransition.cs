using System;
using DG.Tweening;
using StarFunc.Core;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// Camera-zoom Hub→Level transition. Lives in the Hub scene; tweens
    /// <see cref="_camera"/>'s position and orthographic size toward the
    /// tapped level node while fading the shared <see cref="ITransitionOverlay"/>
    /// to opaque. <see cref="ZoomOut"/> runs the reverse pair so the camera
    /// pulls back as the screen reveals.
    /// </summary>
    public class LevelEntryTransition : MonoBehaviour, ILevelEntryTransition
    {
        [Tooltip("Hub camera that gets tweened. If left blank we fall back to " +
                 "Camera.main at runtime — fine for the standard setup.")]
        [SerializeField] Camera _camera;

        [Tooltip("Optional CanvasGroup for the Hub UI. Faded out alongside the " +
                 "camera tween so the level path doesn't pop while the camera " +
                 "moves. Leave null if the transition overlay alone is enough.")]
        [SerializeField] CanvasGroup _hubCanvasGroup;

        [Tooltip("Total tween duration in seconds for both ZoomIn and ZoomOut.")]
        [SerializeField] float _duration = 0.55f;

        [Tooltip("Multiplier applied to the camera's original orthographic size " +
                 "at the end of ZoomIn. 0.45 = camera ends ~half as wide, i.e. " +
                 "roughly 2× zoom.")]
        [SerializeField, Range(0.1f, 1f)] float _zoomedOrthoMultiplier = 0.45f;

        [Tooltip("Easing applied to both position and orthoSize tweens.")]
        [SerializeField] Ease _ease = Ease.InOutQuad;

        Vector3 _originalCameraPos;
        float _originalOrthoSize;
        bool _capturedOriginal;

        Tween _posTween;
        Tween _sizeTween;
        Tween _hubFadeTween;

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
            var cam = ResolveCamera();
            if (cam == null)
            {
                FadeOverlayIn(onComplete);
                return;
            }

            CaptureOriginalOnce(cam);
            KillTweens();

            // Keep the camera's z (depth) — only ease the xy plane toward
            // the node so the orthographic frame zooms cleanly without
            // jumping the near/far plane.
            var target = new Vector3(worldPos.x, worldPos.y, _originalCameraPos.z);
            _posTween = cam.transform
                .DOMove(target, _duration)
                .SetEase(_ease)
                .SetUpdate(true);

            float targetSize = _originalOrthoSize * _zoomedOrthoMultiplier;
            _sizeTween = DOTween
                .To(() => cam.orthographicSize, v => cam.orthographicSize = v,
                    targetSize, _duration)
                .SetEase(_ease)
                .SetUpdate(true);

            if (_hubCanvasGroup != null)
            {
                _hubFadeTween = DOTween
                    .To(() => _hubCanvasGroup.alpha,
                        v => _hubCanvasGroup.alpha = v, 0f, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true);
            }

            FadeOverlayIn(onComplete);
        }

        public void ZoomOut(Action onComplete)
        {
            var cam = ResolveCamera();
            if (!_capturedOriginal || cam == null)
            {
                FadeOverlayOut(onComplete);
                return;
            }

            KillTweens();

            _posTween = cam.transform
                .DOMove(_originalCameraPos, _duration)
                .SetEase(_ease)
                .SetUpdate(true);

            _sizeTween = DOTween
                .To(() => cam.orthographicSize, v => cam.orthographicSize = v,
                    _originalOrthoSize, _duration)
                .SetEase(_ease)
                .SetUpdate(true);

            if (_hubCanvasGroup != null)
            {
                _hubFadeTween = DOTween
                    .To(() => _hubCanvasGroup.alpha,
                        v => _hubCanvasGroup.alpha = v, 1f, _duration)
                    .SetEase(_ease)
                    .SetUpdate(true);
            }

            FadeOverlayOut(onComplete);
        }

        Camera ResolveCamera()
        {
            if (_camera != null) return _camera;
            _camera = Camera.main;
            return _camera;
        }

        void CaptureOriginalOnce(Camera cam)
        {
            if (_capturedOriginal) return;
            _originalCameraPos = cam.transform.position;
            _originalOrthoSize = cam.orthographic ? cam.orthographicSize : 5f;
            _capturedOriginal = true;
        }

        static void FadeOverlayIn(Action onComplete)
        {
            if (ServiceLocator.Contains<ITransitionOverlay>())
                ServiceLocator.Get<ITransitionOverlay>().TransitionIn(onComplete);
            else
                onComplete?.Invoke();
        }

        static void FadeOverlayOut(Action onComplete)
        {
            if (ServiceLocator.Contains<ITransitionOverlay>())
                ServiceLocator.Get<ITransitionOverlay>().TransitionOut(onComplete);
            else
                onComplete?.Invoke();
        }

        void KillTweens()
        {
            if (_posTween != null && _posTween.IsActive()) _posTween.Kill();
            if (_sizeTween != null && _sizeTween.IsActive()) _sizeTween.Kill();
            if (_hubFadeTween != null && _hubFadeTween.IsActive()) _hubFadeTween.Kill();
            _posTween = null;
            _sizeTween = null;
            _hubFadeTween = null;
        }
    }
}
