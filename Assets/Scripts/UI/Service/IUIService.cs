using StarFunc.Data;

namespace StarFunc.UI
{
    public interface IUIService
    {
        void ShowScreen<T>() where T : UIScreen;
        void HideScreen<T>() where T : UIScreen;
        T GetScreen<T>() where T : UIScreen;
        void ShowPopup<T>(PopupData data) where T : UIPopup;
        void HideAllPopups();
    }
}
