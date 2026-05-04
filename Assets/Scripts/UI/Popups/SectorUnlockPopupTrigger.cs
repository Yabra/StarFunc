using StarFunc.Data;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// Always-active hub-side bridge between the <c>OnSectorUnlocked</c> SO event
    /// and <see cref="SectorUnlockPopup"/>. Lives separately from the popup so
    /// it can listen for events while the popup is hidden (an inactive
    /// GameObject's <c>OnEnable</c> doesn't fire).
    /// </summary>
    public class SectorUnlockPopupTrigger : MonoBehaviour
    {
        [SerializeField] SectorUnlockPopup _popup;
        [SerializeField] SectorDataEvent _onSectorUnlocked;

        void OnEnable()
        {
            if (_onSectorUnlocked) _onSectorUnlocked.AddListener(HandleSectorUnlocked);
        }

        void OnDisable()
        {
            if (_onSectorUnlocked) _onSectorUnlocked.RemoveListener(HandleSectorUnlocked);
        }

        void HandleSectorUnlocked(SectorData sector)
        {
            if (_popup == null || sector == null) return;
            _popup.Show(sector);
        }
    }
}
