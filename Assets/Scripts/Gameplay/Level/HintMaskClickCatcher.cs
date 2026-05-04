using UnityEngine;
using UnityEngine.EventSystems;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Routes taps anywhere on the mandatory-hint mask overlay back to the
    /// owning <see cref="HintPopup"/>. Keeps the dismiss-by-tap behaviour
    /// consistent regardless of where the player taps within the dimmed area.
    /// </summary>
    public class HintMaskClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] HintPopup _hintPopup;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hintPopup) _hintPopup.Hide();
        }
    }
}
