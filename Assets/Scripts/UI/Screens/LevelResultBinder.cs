using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.UI
{
    public class LevelResultBinder : MonoBehaviour
    {
        [SerializeField] private UIService _uiService;
        [SerializeField] private LevelResultEvent _onLevelCompleted;
        [SerializeField] private GameEvent _onLevelFailed;
        [SerializeField] private LevelResultScreen _screen;

        private SceneFlowManager _sceneFlowManager;

        private void OnEnable()
        {
            _sceneFlowManager = ServiceLocator.Contains<SceneFlowManager>()
                ? ServiceLocator.Get<SceneFlowManager>()
                : null;

            if (_sceneFlowManager == null)
                Debug.LogWarning("[LevelResultBinder] SceneFlowManager not registered — Next/Retry/Hub buttons will be no-ops. Did you boot from Boot.unity?");

            if (_onLevelCompleted) _onLevelCompleted.AddListener(HandleCompleted);
            if (_onLevelFailed) _onLevelFailed.AddListener(HandleFailed);

            if (_screen)
            {
                _screen.OnNextClicked += HandleNext;
                _screen.OnRetryClicked += HandleRetry;
                _screen.OnHubClicked += HandleHub;
            }
        }

        private void OnDisable()
        {
            if (_onLevelCompleted) _onLevelCompleted.RemoveListener(HandleCompleted);
            if (_onLevelFailed) _onLevelFailed.RemoveListener(HandleFailed);

            if (_screen)
            {
                _screen.OnNextClicked -= HandleNext;
                _screen.OnRetryClicked -= HandleRetry;
                _screen.OnHubClicked -= HandleHub;
            }
        }

        private void HandleCompleted(LevelResult result)
        {
            if (!_screen || !_uiService) return;

            // Activate the screen first, then push data. Setup() starts a
            // coroutine on StarRatingDisplay for the star-fill animation —
            // StartCoroutine throws if the host GameObject is inactive.
            _uiService.ShowScreen<LevelResultScreen>();
            _screen.Setup(result);
            _screen.SetConstellationPreview(ResolveConstellationSprite(result));
        }

        /// <summary>
        /// Picks which constellation sprite the result screen should show.
        /// Restored variant only when the player just won the LAST level of
        /// the sector — that's the celebration moment. Failures and mid-
        /// sector wins fall back to the base (unlit) sprite.
        /// </summary>
        Sprite ResolveConstellationSprite(LevelResult result)
        {
            var sector = SectorData.ActiveSector;
            if (sector == null) return null;

            bool isFinalLevel = false;
            if (sector.Levels != null && sector.Levels.Length > 0)
            {
                var finishedLevel = LevelData.ActiveLevel;
                isFinalLevel = finishedLevel != null
                               && sector.Levels[sector.Levels.Length - 1] == finishedLevel;
            }

            bool celebrate = !result.LevelFailed && isFinalLevel;
            // Fallback to the base sprite if a designer hasn't authored a
            // restored variant yet — never end up with a null Image.sprite.
            Sprite restored = sector.ConstellationRestoredSprite;
            return celebrate && restored != null ? restored : sector.ConstellationSprite;
        }

        private void HandleFailed()
        {
            // LevelController also raises OnLevelCompleted with LevelFailed=true on failure,
            // so the screen is shown via HandleCompleted. This hook stays for audio/analytics.
        }

        private void HandleNext()
        {
            if (_sceneFlowManager) _sceneFlowManager.LoadNextLevel();
        }

        private void HandleRetry()
        {
            if (!_sceneFlowManager) return;

            // Gate retry on lives like the Hub-side LevelLauncher does.
            // Without this, RetryLevel would unload + reload the Level scene,
            // and the new LevelController.Start would catch 0 lives mid-load
            // (when SceneFlowManager._isLevelLoaded is briefly false) and the
            // defensive unload call would no-op — leaving the player stuck.
            if (ServiceLocator.Contains<ILivesService>()
                && !ServiceLocator.Get<ILivesService>().HasLives())
            {
                Debug.Log("[LevelResultBinder] Retry blocked — no lives. Showing NoLivesPopup.");
                if (_uiService) _uiService.ShowPopup<NoLivesPopup>(null);
                return;
            }

            _sceneFlowManager.RetryLevel();
        }

        private void HandleHub()
        {
            if (_sceneFlowManager) _sceneFlowManager.ReturnToHub();
        }
    }
}
