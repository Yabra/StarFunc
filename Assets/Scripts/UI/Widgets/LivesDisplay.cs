using DG.Tweening;
using TMPro;
using UnityEngine;

namespace StarFunc.UI
{
    public class LivesDisplay : MonoBehaviour
    {
        const float BadgeAppearDuration = 0.35f;

        [SerializeField] TMP_Text _livesText;
        [SerializeField] GameObject _notificationBadge;

        Tween _badgeTween;

        public void SetLives(int count)
        {
            _livesText.text = $"♥ {count}";
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
