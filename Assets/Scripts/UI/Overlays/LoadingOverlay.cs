using StarFunc.Core;

namespace StarFunc.UI
{
    /// <summary>
    /// Stub loading overlay: fullscreen darkening + "Loading..." text.
    /// Lives on a DontDestroyOnLoad object. Full implementation — Task 3.8.
    /// </summary>
    public class LoadingOverlay : UIScreen, ILoadingOverlay
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<ILoadingOverlay>(this);
            Hide();
        }
    }
}
