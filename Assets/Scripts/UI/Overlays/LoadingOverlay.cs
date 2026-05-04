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

        Coroutine _delayedShow;
        Tween _fadeTween;

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
            float p = Mathf.Clamp01(progress);
            if (_progressFill) _progressFill.fillAmount = p;
            if (_progressText) _progressText.text = $"{Mathf.RoundToInt(p * 100f)}%";
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
        }
    }
}
