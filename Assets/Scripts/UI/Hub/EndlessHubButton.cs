using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Hub button that opens <see cref="EndlessPopup"/> so the player can
    /// pick difficulty, task type, and an optional seed before rolling a
    /// runtime endless-mode level. Launch logic + Hub UI hide/restore live
    /// in the popup itself; this component is just a thin entry point.
    /// </summary>
    public class EndlessHubButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] UIService _uiService;

        void OnEnable()
        {
            if (_button) _button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            if (_button) _button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick()
        {
            if (_uiService == null)
            {
                Debug.LogWarning("[EndlessHubButton] UIService reference missing.");
                return;
            }

            _uiService.ShowPopup<EndlessPopup>(null);
        }
    }
}
