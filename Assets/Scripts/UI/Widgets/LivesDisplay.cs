using DG.Tweening;
using StarFunc.Core;
using StarFunc.Infrastructure;
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

        [Tooltip("Optional ScriptableObject event raised whenever the lives " +
                 "count changes. Subscribed in OnEnable for live updates; " +
                 "leave null if you'd rather drive the widget by hand via " +
                 "SetLives().")]
        [SerializeField] GameEvent<int> _onLivesChanged;

        Tween _badgeTween;
        bool _eventHookSubscribed;

        void OnEnable()
        {
            // Pull the authoritative initial count from the service so the
            // HUD doesn't display a stale or hard-coded value while we wait
            // for the next OnLivesChanged raise.
            if (ServiceLocator.Contains<ILivesService>())
                SetLives(ServiceLocator.Get<ILivesService>().GetCurrentLives());

            if (_onLivesChanged && !_eventHookSubscribed)
            {
                _onLivesChanged.AddListener(SetLives);
                _eventHookSubscribed = true;
            }
        }

        void OnDisable()
        {
            if (_onLivesChanged && _eventHookSubscribed)
            {
                _onLivesChanged.RemoveListener(SetLives);
                _eventHookSubscribed = false;
            }
        }

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
