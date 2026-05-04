using System;
using DG.Tweening;
using StarFunc.Core;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// DontDestroyOnLoad fullscreen color panel for screen-switch transitions.
    /// Drives a <see cref="CanvasGroup"/> alpha tween via DOTween. Used by
    /// <c>UIService.ShowScreen&lt;T&gt;</c> for fade-cover-fade and by
    /// <c>SceneFlowManager</c> to wrap scene loads.
    /// </summary>
    public class TransitionOverlay : MonoBehaviour, ITransitionOverlay
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] float _duration = 0.25f;

        Tween _tween;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<ITransitionOverlay>(this);

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void TransitionIn(Action onComplete)
        {
            KillTween();

            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _canvasGroup.blocksRaycasts = true;
            _tween = DOTween
                .To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, 1f, _duration)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void TransitionOut(Action onComplete)
        {
            KillTween();

            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _tween = DOTween
                .To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, 0f, _duration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        void KillTween()
        {
            if (_tween != null && _tween.IsActive())
                _tween.Kill();
            _tween = null;
        }

        void OnDestroy() => KillTween();
    }
}
