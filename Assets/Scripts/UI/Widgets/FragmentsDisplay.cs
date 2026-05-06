using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class FragmentsDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _fragmentsText;
        [Tooltip("Optional fragment/diamond icon Image — designer-assigned " +
                 "sprite, sits next to the count.")]
        [SerializeField] Image _icon;
        [Tooltip("string.Format pattern for the count. Default is '{0}'; the " +
                 "old default '◆ {0}' baked the diamond glyph into the text " +
                 "and most project fonts don't ship it.")]
        [SerializeField] string _format = "{0}";

        [Header("Insufficient-Funds Shake")]
        [Tooltip("Tint the count text flashes to when the player tries to " +
                 "spend more than they have. Returns to the original colour " +
                 "after the shake completes.")]
        [SerializeField] Color _shakeFlashColor = new(1f, 0.35f, 0.35f);
        [Tooltip("Horizontal shake amplitude in canvas units.")]
        [SerializeField] float _shakeAmplitude = 14f;

        RectTransform _rt;
        Vector2 _basePos;
        Color _baseTextColor;
        bool _baselineCaptured;
        Sequence _shakeSeq;

        void Awake()
        {
            CaptureBaselineOnce();
        }

        void OnDestroy()
        {
            KillShake();
        }

        public void SetFragments(int count)
        {
            if (_fragmentsText)
                _fragmentsText.text = string.Format(_format, count);
            _ = _icon; // reserved for future tinting / pulse on increase
        }

        /// <summary>
        /// Quick horizontal shake + colour flash on the count text. Called by
        /// ShopItemWidget when the player taps Buy on an item they can't
        /// afford — gives an immediate "you don't have enough" cue without a
        /// blocking dialog.
        /// </summary>
        public void PlayInsufficientShake()
        {
            CaptureBaselineOnce();
            if (_rt == null) return;

            KillShake();
            // Snap back to baseline before shaking so consecutive taps don't
            // accumulate offsets/colours.
            _rt.anchoredPosition = _basePos;
            if (_fragmentsText) _fragmentsText.color = _baseTextColor;

            float a = _shakeAmplitude;
            var seq = DOTween.Sequence();
            seq.Append(TweenAnchorX(_basePos.x + a, 0.06f));
            seq.Append(TweenAnchorX(_basePos.x - a, 0.10f));
            seq.Append(TweenAnchorX(_basePos.x + a * 0.6f, 0.08f));
            seq.Append(TweenAnchorX(_basePos.x, 0.05f));

            if (_fragmentsText)
            {
                seq.Insert(0f, DOTween
                    .To(() => _fragmentsText.color,
                        c => _fragmentsText.color = c,
                        _shakeFlashColor, 0.12f));
                seq.Insert(0.12f, DOTween
                    .To(() => _fragmentsText.color,
                        c => _fragmentsText.color = c,
                        _baseTextColor, 0.18f));
            }

            seq.SetUpdate(true);
            _shakeSeq = seq;
        }

        Tween TweenAnchorX(float targetX, float duration) =>
            DOTween.To(
                () => _rt.anchoredPosition.x,
                x => _rt.anchoredPosition = new Vector2(x, _basePos.y),
                targetX, duration);

        void CaptureBaselineOnce()
        {
            if (_baselineCaptured) return;
            _rt = transform as RectTransform;
            if (_rt) _basePos = _rt.anchoredPosition;
            if (_fragmentsText) _baseTextColor = _fragmentsText.color;
            _baselineCaptured = true;
        }

        void KillShake()
        {
            if (_shakeSeq != null && _shakeSeq.IsActive())
                _shakeSeq.Kill();
            _shakeSeq = null;
        }
    }
}
