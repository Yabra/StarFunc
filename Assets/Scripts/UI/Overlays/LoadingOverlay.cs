using System.Collections;
using DG.Tweening;
using StarFunc.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// DontDestroyOnLoad fullscreen "loading…" panel with a fade in/out and an
    /// optional progress bar. Used by <c>SceneFlowManager</c> as a safety
    /// screen during slow scene loads (Boot → Hub on cold start, Hub → Level
    /// when network warm-up trips a long path).
    /// </summary>
    public class LoadingOverlay : UIScreen, ILoadingOverlay
    {
        [Header("Visuals")]
        [SerializeField] Image _progressFill;
        [SerializeField] TMP_Text _progressText;
        [SerializeField] float _fadeDuration = 0.2f;
        [Tooltip("Seconds to interpolate the progress bar to a new value. " +
                 "Boot milestones fire faster than the UI redraws, so without " +
                 "a tween the bar looks like it jumps straight to 100%.")]
        [SerializeField] float _progressTweenDuration = 0.3f;

        Coroutine _delayedShow;
        Tween _fadeTween;
        Tween _progressTween;
        float _displayedProgress;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<ILoadingOverlay>(this);
            Hide();
        }

        public override void Show()
        {
            CancelDelayedShow();
            KillFade();

            // Reset progress so a re-shown overlay (Boot → Hub → Level
            // sequence) starts from 0 rather than the previous run's last
            // value still tweening into view.
            ResetProgress();

            gameObject.SetActive(true);
            if (_canvasGroup)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _fadeTween = DOTween
                    .To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, 1f, _fadeDuration)
                    .SetUpdate(true);     // ignore Time.timeScale (paused scenes still fade)
            }
            else
            {
                // No CanvasGroup wired — fall back to instant.
                base.Show();
            }
        }

        public override void Hide()
        {
            CancelDelayedShow();
            KillFade();

            if (_canvasGroup)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _fadeTween = DOTween
                    .To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, 0f, _fadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                base.Hide();
            }
        }

        public void ShowDelayed(float thresholdSeconds)
        {
            CancelDelayedShow();
            if (thresholdSeconds <= 0f)
            {
                Show();
                return;
            }

            // The GameObject is deactivated when Hide() finishes its fade —
            // which means StartCoroutine would fail until Show() runs. Wake it
            // up here, but keep the canvas inert (alpha 0, no raycasts) so the
            // delay window still feels invisible to the player.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            _delayedShow = StartCoroutine(DelayedShowRoutine(thresholdSeconds));
        }

        public void SetProgress(float progress)
        {
            float target = Mathf.Clamp01(progress);

            KillProgressTween();
            // No duration / instant requested → snap. Also avoids spawning a
            // 0-second tween which DOTween treats as a no-op and never updates
            // the visible value.
            if (_progressTweenDuration <= 0f)
            {
                ApplyProgress(target);
                return;
            }

            _progressTween = DOTween
                .To(() => _displayedProgress, ApplyProgress, target, _progressTweenDuration)
                .SetUpdate(true);
        }

        void ApplyProgress(float v)
        {
            _displayedProgress = v;
            if (_progressFill) _progressFill.fillAmount = v;
            if (_progressText) _progressText.text = $"{Mathf.RoundToInt(v * 100f)}%";
        }

        void ResetProgress()
        {
            KillProgressTween();
            ApplyProgress(0f);
        }

        void KillProgressTween()
        {
            if (_progressTween != null && _progressTween.IsActive())
                _progressTween.Kill();
            _progressTween = null;
        }

        IEnumerator DelayedShowRoutine(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _delayedShow = null;
            Show();
        }

        void CancelDelayedShow()
        {
            if (_delayedShow != null)
            {
                StopCoroutine(_delayedShow);
                _delayedShow = null;
            }
        }

        void KillFade()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
                _fadeTween.Kill();
            _fadeTween = null;
        }

        void OnDestroy()
        {
            CancelDelayedShow();
            KillFade();
            KillProgressTween();
        }
    }
}
