using System.Collections;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFunc.Infrastructure
{
    public class SceneFlowManager : MonoBehaviour
    {
        const float LoadingOverlayDelay = 0.25f;

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

        /// <summary>Reload the currently active level (Retry).</summary>
        public void RetryLevel()
        {
            if (!_isLevelLoaded || CurrentLevel == null) return;

            StartCoroutine(RetryLevelRoutine(CurrentLevel));
        }

        /// <summary>Unload the level and return to the Hub scene underneath.</summary>
        public void ReturnToHub() => UnloadLevel();

        // Sector traversal isn't wired up yet, so "Next" falls back to Hub for now.
        // Replace the body once HubScreen / SectorData navigation lands (Task 2.7).
        public void LoadNextLevel() => UnloadLevel();

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
            var transition = GetTransition();

            yield return WaitForTransitionIn(transition);

            overlay?.ShowDelayed(LoadingOverlayDelay);

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            while (!op.isDone)
            {
                overlay?.SetProgress(op.progress);
                yield return null;
            }
            overlay?.SetProgress(1f);
            overlay?.Hide();

            onLoaded?.Invoke();

            transition?.TransitionOut(null);
        }

        IEnumerator RetryLevelRoutine(LevelData level)
        {
            var overlay = GetOverlay();
            var transition = GetTransition();

            yield return WaitForTransitionIn(transition);

            overlay?.ShowDelayed(LoadingOverlayDelay);

            var unload = SceneManager.UnloadSceneAsync("Level");
            while (!unload.isDone)
            {
                overlay?.SetProgress(unload.progress * 0.5f);
                yield return null;
            }

            _isLevelLoaded = false;
            CurrentLevel = level;
            LevelData.ActiveLevel = level;

            var load = SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
            while (!load.isDone)
            {
                overlay?.SetProgress(0.5f + load.progress * 0.5f);
                yield return null;
            }

            SetHubUIActive(false);
            _isLevelLoaded = true;
            overlay?.SetProgress(1f);
            overlay?.Hide();

            transition?.TransitionOut(null);
        }

        IEnumerator UnloadLevelRoutine()
        {
            var overlay = GetOverlay();
            var transition = GetTransition();

            yield return WaitForTransitionIn(transition);

            overlay?.ShowDelayed(LoadingOverlayDelay);

            var op = SceneManager.UnloadSceneAsync("Level");
            while (!op.isDone)
            {
                overlay?.SetProgress(op.progress);
                yield return null;
            }

            SetHubUIActive(true);
            _isLevelLoaded = false;
            CurrentLevel = null;
            LevelData.ActiveLevel = null;

            overlay?.SetProgress(1f);
            overlay?.Hide();

            transition?.TransitionOut(null);
        }

        static IEnumerator WaitForTransitionIn(ITransitionOverlay transition)
        {
            if (transition == null) yield break;
            bool done = false;
            transition.TransitionIn(() => done = true);
            while (!done) yield return null;
        }

        static ILoadingOverlay GetOverlay()
        {
            return ServiceLocator.Contains<ILoadingOverlay>()
                ? ServiceLocator.Get<ILoadingOverlay>()
                : null;
        }

        static ITransitionOverlay GetTransition()
        {
            return ServiceLocator.Contains<ITransitionOverlay>()
                ? ServiceLocator.Get<ITransitionOverlay>()
                : null;
        }

        static void SetHubUIActive(bool active)
        {
            // Hub UI visibility will be managed when HubScreen is implemented.
            // For now this is a no-op placeholder.
        }
    }
}
