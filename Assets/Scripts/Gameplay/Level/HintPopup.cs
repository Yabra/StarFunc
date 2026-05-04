using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Lightweight in-level hint widget — shows a text bubble plus an optional
    /// highlight marker pinned to <c>HintConfig.HighlightPosition</c> (canvas
    /// space). Auto-dismisses after <see cref="_defaultTimeout"/> seconds or on
    /// tap. In <c>mandatory</c> mode (used by Tutorial-type levels per task
    /// 4.5), a fullscreen 4-panel dim cutout focuses the player on the
    /// highlight rect, the auto-timeout is disabled, and a "tap to continue"
    /// label is shown.
    /// Not a <c>UIPopup</c>: this lives in the Gameplay assembly with the
    /// HintSystem so HUD-side popups stay independent.
    /// </summary>
    public class HintPopup : MonoBehaviour, IPointerClickHandler
    {
        [Header("Bubble")]
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] TMP_Text _text;
        [SerializeField] RectTransform _highlight;
        [SerializeField] float _defaultTimeout = 5f;

        [Header("Mandatory mode (Tutorial)")]
        [SerializeField] GameObject _maskRoot;
        [SerializeField] RectTransform _maskTop;
        [SerializeField] RectTransform _maskBottom;
        [SerializeField] RectTransform _maskLeft;
        [SerializeField] RectTransform _maskRight;
        [SerializeField] GameObject _continueLabel;
        [SerializeField] Vector2 _highlightSize = new(220f, 220f);

        Coroutine _hideRoutine;
        bool _mandatory;

        public bool IsVisible => gameObject.activeSelf;

        /// <summary>Fires after Hide() — used by HintSystem to chain mandatory hints.</summary>
        public event Action OnDismissed;

        void Awake()
        {
            HideInternal(invokeEvent: false);
        }

        /// <summary>
        /// Show the hint. <paramref name="highlightCanvasPos"/> is interpreted as
        /// anchored position on this widget's parent canvas; pass <see cref="Vector2.zero"/>
        /// with a null/disabled <c>_highlight</c> for text-only hints.
        /// Pass <paramref name="timeout"/> &lt;= 0 for no auto-hide. When <paramref name="mandatory"/>
        /// is true, the auto-hide is disabled regardless of <paramref name="timeout"/> and the
        /// mask cutout is enabled.
        /// </summary>
        public void Show(string text, Vector2 highlightCanvasPos, float timeout = -1f,
            bool mandatory = false)
        {
            _mandatory = mandatory;

            if (_text) _text.text = text ?? string.Empty;

            bool hasHighlight = highlightCanvasPos != Vector2.zero;
            if (_highlight)
            {
                _highlight.gameObject.SetActive(hasHighlight);
                if (hasHighlight) _highlight.anchoredPosition = highlightCanvasPos;
            }

            ApplyMask(mandatory, highlightCanvasPos, hasHighlight);

            if (_continueLabel) _continueLabel.SetActive(mandatory);

            gameObject.SetActive(true);
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            if (mandatory) return; // mandatory hints stay until tapped
            float t = timeout < 0 ? _defaultTimeout : timeout;
            if (t > 0f) _hideRoutine = StartCoroutine(HideAfter(t));
        }

        public void Hide() => HideInternal(invokeEvent: true);

        public void OnPointerClick(PointerEventData eventData) => Hide();

        void HideInternal(bool invokeEvent)
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_maskRoot) _maskRoot.SetActive(false);
            if (_continueLabel) _continueLabel.SetActive(false);

            gameObject.SetActive(false);

            bool wasMandatory = _mandatory;
            _mandatory = false;

            if (invokeEvent && wasMandatory)
                OnDismissed?.Invoke();
        }

        IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _hideRoutine = null;
            HideInternal(invokeEvent: true);
        }

        // =====================================================================
        // Mask cutout — 4 dim panels framing the highlight rect
        // =====================================================================

        void ApplyMask(bool mandatory, Vector2 center, bool hasHighlight)
        {
            if (_maskRoot == null) return;

            if (!mandatory || !hasHighlight)
            {
                _maskRoot.SetActive(false);
                return;
            }

            _maskRoot.SetActive(true);

            // The mask root is anchored stretched to its parent (Canvas),
            // so its rect equals the canvas. Drive the 4 child panels via
            // anchoredPosition / sizeDelta in canvas-centered space.
            var rt = _maskRoot.transform as RectTransform;
            var parentRT = rt != null ? rt.parent as RectTransform : null;
            if (parentRT == null) return;

            float W = parentRT.rect.width;
            float H = parentRT.rect.height;

            float halfW = _highlightSize.x * 0.5f;
            float halfH = _highlightSize.y * 0.5f;

            float left = center.x - halfW;
            float right = center.x + halfW;
            float top = center.y + halfH;
            float bottom = center.y - halfH;
            float canvasTop = H * 0.5f;
            float canvasBottom = -H * 0.5f;
            float canvasLeft = -W * 0.5f;
            float canvasRight = W * 0.5f;

            SetPanel(_maskTop,
                x: 0f, y: (top + canvasTop) * 0.5f,
                w: W, h: Mathf.Max(0f, canvasTop - top));

            SetPanel(_maskBottom,
                x: 0f, y: (bottom + canvasBottom) * 0.5f,
                w: W, h: Mathf.Max(0f, bottom - canvasBottom));

            SetPanel(_maskLeft,
                x: (left + canvasLeft) * 0.5f, y: center.y,
                w: Mathf.Max(0f, left - canvasLeft), h: _highlightSize.y);

            SetPanel(_maskRight,
                x: (right + canvasRight) * 0.5f, y: center.y,
                w: Mathf.Max(0f, canvasRight - right), h: _highlightSize.y);
        }

        static void SetPanel(RectTransform panel, float x, float y, float w, float h)
        {
            if (!panel) return;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(x, y);
            panel.sizeDelta = new Vector2(w, h);
        }
    }
}
