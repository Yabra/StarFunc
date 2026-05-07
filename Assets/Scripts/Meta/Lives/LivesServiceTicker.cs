using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Drives <see cref="LocalLivesService.Tick"/> every frame so the
    /// auto-restore countdown progresses while the game is running. Without
    /// this the timer only catches up across app launches via
    /// <c>RecalculateAfterOffline</c>; sitting on the NoLivesPopup with the
    /// app open would never tick down. Spawned as a DontDestroyOnLoad host
    /// from BootInitializer alongside the other services.
    /// </summary>
    public class LivesServiceTicker : MonoBehaviour
    {
        LocalLivesService _service;

        public void Bind(LocalLivesService service)
        {
            _service = service;
        }

        void Update()
        {
            // unscaledDeltaTime so a Time.timeScale=0 pause (e.g. PausePopup)
            // doesn't freeze the real-world restore timer.
            _service?.Tick(Time.unscaledDeltaTime);
        }
    }
}
