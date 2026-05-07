using StarFunc.Core;
using StarFunc.Infrastructure;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Hub topbar widget mirroring <see cref="FragmentsDisplay"/> /
    /// <see cref="LivesDisplay"/>, but for the player's paid-hint inventory
    /// (PlayerSaveData.Consumables["hints"]). Pulls the initial count from
    /// <see cref="IShopService"/> on enable, then listens to the
    /// <c>OnHintsChanged</c> SO event for live updates raised by
    /// LocalShopService (purchases) and HintSystem (consumption).
    /// </summary>
    public class HintsDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _hintsText;
        [Tooltip("Optional bulb icon Image — designer-assigned sprite, sits next to the count.")]
        [SerializeField] Image _icon;
        [SerializeField] string _format = "{0}";

        [Tooltip("Raised whenever Consumables['hints'] changes. Subscribed in OnEnable; " +
                 "leave null to drive the widget by hand via SetCount().")]
        [SerializeField] GameEvent<int> _onHintsChanged;

        bool _eventHookSubscribed;

        void OnEnable()
        {
            // Pull the authoritative initial count from the shop service so
            // the topbar doesn't display 0 while waiting for the next change.
            if (ServiceLocator.Contains<IShopService>())
                SetCount(ServiceLocator.Get<IShopService>().GetConsumableCount(LocalShopService.HintsKey));

            if (_onHintsChanged && !_eventHookSubscribed)
            {
                _onHintsChanged.AddListener(SetCount);
                _eventHookSubscribed = true;
            }
        }

        void OnDisable()
        {
            if (_onHintsChanged && _eventHookSubscribed)
            {
                _onHintsChanged.RemoveListener(SetCount);
                _eventHookSubscribed = false;
            }
        }

        public void SetCount(int count)
        {
            if (_hintsText) _hintsText.text = string.Format(_format, count);
            _ = _icon; // reserved for future tinting (e.g. dim when 0).
        }
    }
}
