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
        float _dismissibleAfter;

        public bool IsVisible => gameObject.activeSelf;

        /// <summary>Fires after Hide() — used by HintSystem to chain mandatory hints.</summary>
        public event Action OnDismissed;

        void Awake()
        {
            // Initialise visual state without deactivating the GameObject:
            // SetActive(false) here would re-trigger inside the first Show()
            // (Awake fires when an inactive popup is activated by Show), and
            // the popup would end up off despite Show running through.
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_maskRoot) _maskRoot.SetActive(false);
            if (_continueLabel) _continueLabel.SetActive(false);
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
            bool mandatory = false, float minDisplayTime = 0f,
            Vector2 highlightSizeOverride = default)
        {
            // Activate the popup root first. On the very first Show this also
            // fires Awake(), which initialises canvas-group / mask / continue
            // state — we then override that state below. Doing SetActive(true)
            // *after* ApplyMask would let Awake clobber the mask-active flag.
            gameObject.SetActive(true);

            _mandatory = mandatory;
            _dismissibleAfter = minDisplayTime > 0f
                ? Time.unscaledTime + minDisplayTime
                : 0f;

            if (_text) _text.text = text ?? string.Empty;

            bool hasHighlight = highlightCanvasPos != Vector2.zero;
            if (_highlight)
            {
                _highlight.gameObject.SetActive(hasHighlight);
                if (hasHighlight) PlaceAtCanvasPos(_highlight, highlightCanvasPos);
            }

            Vector2 effectiveSize = highlightSizeOverride.x > 0f && highlightSizeOverride.y > 0f
                ? highlightSizeOverride
                : _highlightSize;
            ApplyMask(mandatory, highlightCanvasPos, hasHighlight, effectiveSize);

            ApplyHighlightSize(effectiveSize);

            if (_continueLabel) _continueLabel.SetActive(mandatory);

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

        /// <summary>
        /// Dismiss-on-tap entry point used by both the popup itself and the
        /// surrounding mask click-catcher. Respects the grace period set on
        /// <see cref="Show"/> so reflex taps right after a hint pops up don't
        /// kill it before the player can read.
        /// </summary>
        public void TryDismiss()
        {
            if (Time.unscaledTime < _dismissibleAfter) return;
            Hide();
        }

        public void OnPointerClick(PointerEventData eventData) => TryDismiss();

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

        void ApplyMask(bool mandatory, Vector2 center, bool hasHighlight, Vector2 holeSize)
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

            float halfW = holeSize.x * 0.5f;
            float halfH = holeSize.y * 0.5f;

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
                w: Mathf.Max(0f, left - canvasLeft), h: holeSize.y);

            SetPanel(_maskRight,
                x: (right + canvasRight) * 0.5f, y: center.y,
                w: Mathf.Max(0f, canvasRight - right), h: holeSize.y);
        }

        void ApplyHighlightSize(Vector2 size)
        {
            if (_highlight == null) return;
            _highlight.sizeDelta = size;
        }

        static void SetPanel(RectTransform panel, float x, float y, float w, float h)
        {
            if (!panel) return;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(x, y);
            panel.sizeDelta = new Vector2(w, h);
        }

        /// <summary>
        /// Convert a canvas-centered position (the same space the mask hole uses)
        /// into the marker's local anchoredPosition, regardless of where the
        /// marker is parented in the canvas hierarchy.
        /// </summary>
        static void PlaceAtCanvasPos(RectTransform target, Vector2 canvasPos)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                target.anchoredPosition = canvasPos;
                return;
            }

            var canvasRT = canvas.transform as RectTransform;
            var parentRT = target.parent as RectTransform;
            if (canvasRT == null || parentRT == null)
            {
                target.anchoredPosition = canvasPos;
                return;
            }

            // Canvas-centered → world (TransformPoint treats local pos as canvas-centered
            // because the canvas RT is centered on its own origin) → parent-local.
            Vector3 world = canvasRT.TransformPoint(new Vector3(canvasPos.x, canvasPos.y, 0f));
            Vector3 local = parentRT.InverseTransformPoint(world);

            // anchoredPosition is relative to the anchor point, while local-position
            // is relative to the parent's pivot. anchoredPosition = local - (anchor
            // - parentPivot) * parentRect.size — using parentRT.pivot, not 0.5,
            // because pivot != centre on most rects (HintPopup is top-pivoted).
            Vector2 anchorMid = (target.anchorMin + target.anchorMax) * 0.5f;
            Vector2 anchorOffset = new(
                (anchorMid.x - parentRT.pivot.x) * parentRT.rect.width,
                (anchorMid.y - parentRT.pivot.y) * parentRT.rect.height);

            target.anchoredPosition = new Vector2(local.x, local.y) - anchorOffset;
        }
    }
}
