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
        const string LevelSceneName = "Level";

        bool _isLevelLoaded;

        public LevelData CurrentLevel { get; private set; }

        void OnEnable()
        {
            // Reset _isLevelLoaded whenever the Level scene goes away,
            // regardless of HOW it went away (explicit UnloadLevel,
            // Single-mode replacement via LoadScene("Hub"), etc.). This
            // manager is DontDestroyOnLoad, so the flag would otherwise
            // stay true forever after a Hub-via-pause exit and the next
            // LoadLevel call would early-return on its very first line.
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene.name != LevelSceneName) return;
            _isLevelLoaded = false;
            CurrentLevel = null;
            LevelData.ActiveLevel = null;
        }

        /// <summary>
        /// Loads Level scene additively on top of Hub and hides Hub UI.
        /// </summary>
        public void LoadLevel(LevelData level)
        {
            if (_isLevelLoaded) return;

            CurrentLevel = level;
            LevelData.ActiveLevel = level;
            // SectorScreen runs LevelEntryTransition.ZoomIn before firing the
            // selected event, so by the time we get here the screen is already
            // covered. Pass coverAlreadyShown=true so we don't run another
            // (redundant) TransitionIn fade.
            StartCoroutine(LoadSceneRoutine("Level", LoadSceneMode.Additive,
                onLoaded: () =>
                {
                    SetHubUIActive(false);
                    // Disable Hub's camera so Camera.main resolves to the
                    // Level camera unambiguously — both scenes ship cameras
                    // tagged MainCamera, and the persistent background uses
                    // Camera.main to size itself. With Hub's camera off the
                    // sprite gets Level's ortho size, not Hub's.
                    SetHubCameraEnabled(false);
                    _isLevelLoaded = true;
                },
                coverAlreadyShown: true));
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
            System.Action onLoaded = null, bool coverAlreadyShown = false)
        {
            var overlay = GetOverlay();
            var transition = GetTransition();

            if (!coverAlreadyShown)
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
            var levelEntry = GetLevelEntryTransition();

            overlay?.ShowDelayed(LoadingOverlayDelay);

            var op = SceneManager.UnloadSceneAsync("Level");
            while (!op.isDone)
            {
                overlay?.SetProgress(op.progress);
                yield return null;
            }

            SetHubUIActive(true);
            // Re-enable Hub's camera before the ZoomOut starts — otherwise
            // there's nothing rendering Hub once Level is gone.
            SetHubCameraEnabled(true);
            _isLevelLoaded = false;
            CurrentLevel = null;
            LevelData.ActiveLevel = null;

            overlay?.SetProgress(1f);
            overlay?.Hide();

            // Pull the camera back to its original framing. With the same
            // background shared across Hub/Level the unload reads as a
            // continuous dolly-out — no overlay fade needed. Falls back to
            // TransitionOut if a scene hasn't wired up LevelEntryTransition.
            if (levelEntry != null)
                levelEntry.ZoomOut(null);
            else
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

        static ILevelEntryTransition GetLevelEntryTransition()
        {
            return ServiceLocator.Contains<ILevelEntryTransition>()
                ? ServiceLocator.Get<ILevelEntryTransition>()
                : null;
        }

        static void SetHubUIActive(bool active)
        {
            // Hub UI visibility will be managed when HubScreen is implemented.
            // For now this is a no-op placeholder.
        }

        static void SetHubCameraEnabled(bool enabled)
        {
            var hubScene = SceneManager.GetSceneByName("Hub");
            if (!hubScene.IsValid() || !hubScene.isLoaded) return;

            foreach (var root in hubScene.GetRootGameObjects())
            {
                var cam = root.GetComponentInChildren<Camera>(includeInactive: true);
                if (cam == null) continue;
                cam.enabled = enabled;
                return;
            }
        }
    }
}
