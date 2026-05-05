using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class LivesDisplay : MonoBehaviour
    {
        const float BadgeAppearDuration = 0.35f;

        [SerializeField] TMP_Text _livesText;
        [Tooltip("Optional heart icon Image — designer-assigned sprite, sits next " +
                 "to the count. Leave null if the sprite is positioned in the " +
                 "scene without a script reference.")]
        [SerializeField] Image _icon;
        [SerializeField] GameObject _notificationBadge;

        Tween _badgeTween;

        public void SetLives(int count)
        {
            // Heart glyph used to be embedded in text — it's now a sibling
            // Image because most fonts in the project don't ship that codepoint.
            if (_livesText) _livesText.text = count.ToString();
            _ = _icon; // reserved for future tinting (e.g. dim when 0 lives)
        }

        /// <summary>
        /// Show or hide the "lives just refilled" red-dot. Animated pop-in via
        /// DOTween scale (0 → 1, OutBack); hide is instant.
        /// </summary>
        public void SetBadge(bool show)
        {
            if (_notificationBadge == null) return;

            KillBadgeTween();

            if (show)
            {
                bool wasVisible = _notificationBadge.activeSelf;
                _notificationBadge.SetActive(true);

                if (!wasVisible)
                {
                    var t = _notificationBadge.transform;
                    t.localScale = Vector3.zero;
                    _badgeTween = t.DOScale(1f, BadgeAppearDuration).SetEase(Ease.OutBack);
                }
            }
            else
            {
                _notificationBadge.transform.localScale = Vector3.one;
                _notificationBadge.SetActive(false);
            }
        }

        void KillBadgeTween()
        {
            if (_badgeTween != null && _badgeTween.IsActive())
                _badgeTween.Kill();
            _badgeTween = null;
        }

        void OnDestroy() => KillBadgeTween();
    }
}
