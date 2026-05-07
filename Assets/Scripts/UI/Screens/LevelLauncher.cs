using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Infrastructure;
using UnityEngine;

namespace StarFunc.UI
{
    public class LevelLauncher : MonoBehaviour
    {
        [SerializeField] LevelDataEvent _onLevelSelected;
        [SerializeField] UIService _uiService;

        SceneFlowManager _sceneFlowManager;
        ILivesService _livesService;

        void OnEnable()
        {
            _sceneFlowManager = ServiceLocator.Contains<SceneFlowManager>()
                ? ServiceLocator.Get<SceneFlowManager>()
                : null;

            if (_sceneFlowManager == null)
                Debug.LogWarning("[LevelLauncher] SceneFlowManager not registered — level select will be a no-op. Did you boot from Boot.unity?");

            _livesService = ServiceLocator.Contains<ILivesService>()
                ? ServiceLocator.Get<ILivesService>()
                : null;

            if (_onLevelSelected) _onLevelSelected.AddListener(HandleLevelSelected);
        }

        void OnDisable()
        {
            if (_onLevelSelected) _onLevelSelected.RemoveListener(HandleLevelSelected);
        }

        void HandleLevelSelected(LevelData level)
        {
            if (level == null) return;

            // Gate on lives: empty inventory → show the No-Lives popup so the
            // player can wait, restore, or buy. We skip loading the Level
            // scene entirely instead of letting it boot and bounce back.
            if (_livesService != null && !_livesService.HasLives())
            {
                Debug.Log("[LevelLauncher] Level launch blocked — no lives. Showing NoLivesPopup.");
                if (_uiService != null)
                    _uiService.ShowPopup<NoLivesPopup>(null);
                return;
            }

            if (_sceneFlowManager) _sceneFlowManager.LoadLevel(level);
        }
    }
}
