using System.Collections;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFunc.Infrastructure
{
    public class SceneFlowManager : MonoBehaviour
    {
        bool _isLevelLoaded;

        public LevelData CurrentLevel { get; private set; }

        /// <summary>
        /// Loads Level scene additively on top of Hub and hides Hub UI.
        /// </summary>
        public void LoadLevel(LevelData level)
        {
            if (_isLevelLoaded) return;

            CurrentLevel = level;
            LevelData.ActiveLevel = level;
            StartCoroutine(LoadSceneRoutine("Level", LoadSceneMode.Additive, onLoaded: () =>
            {
                SetHubUIActive(false);
                _isLevelLoaded = true;
            }));
        }

        /// <summary>
        /// Unloads the Level scene and restores Hub UI.
        /// </summary>
        public void UnloadLevel()
        {
            if (!_isLevelLoaded) return;

            StartCoroutine(UnloadLevelRoutine());
        }

        /// <summary>
        /// Full scene replacement (used for Boot → Hub).
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single));
        }

        IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode,
            System.Action onLoaded = null)
        {
            var overlay = GetOverlay();
            overlay?.Show();

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            while (!op.isDone)
                yield return null;

            overlay?.Hide();
            onLoaded?.Invoke();
        }

        IEnumerator UnloadLevelRoutine()
        {
            var overlay = GetOverlay();
            overlay?.Show();

            var op = SceneManager.UnloadSceneAsync("Level");
            while (!op.isDone)
                yield return null;

            SetHubUIActive(true);
            _isLevelLoaded = false;
            CurrentLevel = null;
            LevelData.ActiveLevel = null;

            overlay?.Hide();
        }

        static ILoadingOverlay GetOverlay()
        {
            return ServiceLocator.Contains<ILoadingOverlay>()
                ? ServiceLocator.Get<ILoadingOverlay>()
                : null;
        }

        static void SetHubUIActive(bool active)
        {
            // Hub UI visibility will be managed when HubScreen is implemented.
            // For now this is a no-op placeholder.
        }
    }
}
