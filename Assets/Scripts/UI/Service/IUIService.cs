using StarFunc.Data;

namespace StarFunc.UI
{
    public interface IUIService
    {
        void ShowScreen<T>() where T : UIScreen;
        /// <summary>
        /// Show a screen without running the global TransitionOverlay
        /// fade. Used by callers that have already orchestrated their
        /// own transition (e.g. <c>HubSectorTransition</c>) and just need
        /// to register the swap with the screen stack.
        /// </summary>
        void ShowScreen<T>(bool useTransition) where T : UIScreen;
        void HideScreen<T>() where T : UIScreen;
        T GetScreen<T>() where T : UIScreen;
        void ShowPopup<T>(PopupData data) where T : UIPopup;
        T GetPopup<T>() where T : UIPopup;
        void HideAllPopups();
    }
}
