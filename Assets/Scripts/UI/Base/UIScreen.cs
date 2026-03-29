using UnityEngine;

namespace StarFunc.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;

        public bool IsVisible => gameObject.activeSelf;

        public virtual void Show()
        {
            gameObject.SetActive(true);

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

            gameObject.SetActive(false);
        }
    }
}
