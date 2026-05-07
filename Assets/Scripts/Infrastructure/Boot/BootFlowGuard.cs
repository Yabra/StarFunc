using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Editor convenience: ensures every scene was reached via the canonical
    /// Boot → Hub → Level flow. Drop a GameObject with this component into
    /// each scene; tick <c>_isBootScene</c> only on the Boot scene's instance.
    /// In a non-Boot scene opened directly (Editor "Play" with Hub or Level
    /// active), the guard redirects to Boot in Single mode so services are
    /// initialized properly. In production builds the flag is set on the very
    /// first frame, so all subsequent scenes pass through silently.
    /// </summary>
    public class BootFlowGuard : MonoBehaviour
    {
        [Tooltip("Tick on the Boot scene's guard only. Non-Boot scenes leave this off " +
                 "and rely on the Boot guard to mark the flow as initialized.")]
        [SerializeField] bool _isBootScene;

        [Tooltip("Scene to redirect to when this scene was opened without Boot running first.")]
        [SerializeField] string _bootSceneName = "Boot";

        static bool _bootCompleted;

        // Reset across play sessions so this still works when "Enter Play Mode
        // Options" is configured to skip the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => _bootCompleted = false;

        void Awake()
        {
            if (_isBootScene)
            {
                _bootCompleted = true;
                return;
            }

            if (_bootCompleted) return;

            Debug.LogWarning(
                $"[BootFlowGuard] '{gameObject.scene.name}' opened without Boot — " +
                $"redirecting to '{_bootSceneName}'.");
            SceneManager.LoadScene(_bootSceneName, LoadSceneMode.Single);
        }
    }
}
