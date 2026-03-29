using StarFunc.Data;
using UnityEngine;

namespace StarFunc.UI
{
    public abstract class UIPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] GameObject _dimBackground;

        public bool IsVisible => gameObject.activeSelf;

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
        }
    }
}
