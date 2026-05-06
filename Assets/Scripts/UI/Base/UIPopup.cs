using System;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.UI
{
    public abstract class UIPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] GameObject _dimBackground;

        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// Fires after <see cref="Hide"/> finishes. Used by callers that
        /// stack popups (e.g. PausePopup hides itself while SettingsPopup
        /// is up, then re-shows once SettingsPopup closes).
        /// </summary>
        public event Action OnHidden;

        public virtual void Show(PopupData data)
        {
            gameObject.SetActive(true);

            if (_dimBackground)
                _dimBackground.SetActive(true);

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public virtual void Hide()
        {
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_dimBackground)
                _dimBackground.SetActive(false);

            gameObject.SetActive(false);
            OnHidden?.Invoke();
        }
    }
}
