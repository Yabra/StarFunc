using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class SectorScreen : UIScreen
    {
        [Header("Sector Info")]
        [SerializeField] TMP_Text _sectorNameText;
        [SerializeField] TMP_Text _sectorStarsText;

        [Header("Level Path")]
        [SerializeField] ScrollRect _scrollRect;
        [SerializeField] Transform _levelContainer;
        [SerializeField] LevelNodeWidget _levelNodePrefab;

        [Header("Navigation")]
        [SerializeField] Button _backButton;

        [Header("Events")]
        [SerializeField] GameEvent<LevelData> _onLevelSelected;

        SectorData _currentSector;
        LevelNodeWidget[] _nodes;

        IProgressionService _progression;
        IUIService _uiService;

        public SectorData CurrentSector => _currentSector;

        void Start()
        {
            _progression = ServiceLocator.Get<IProgressionService>();
            _uiService = ServiceLocator.Get<IUIService>();

            if (_backButton)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        public void SetSector(SectorData sector)
        {
            _currentSector = sector;
            EnsureServices();
            BuildLevelNodes();
            RefreshAll();
        }

        public override void Show()
        {
            base.Show();
            if (_currentSector != null)
                RefreshAll();
        }

        void EnsureServices()
        {
            _progression ??= ServiceLocator.Get<IProgressionService>();
            _uiService ??= ServiceLocator.Get<IUIService>();
        }

        void BuildLevelNodes()
        {
            ClearNodes();

            if (_currentSector?.Levels == null || _levelNodePrefab == null) return;

            _nodes = new LevelNodeWidget[_currentSector.Levels.Length];

            for (int i = 0; i < _currentSector.Levels.Length; i++)
            {
                var node = Instantiate(_levelNodePrefab, _levelContainer);
                node.OnClicked += OnLevelClicked;

                // Hide connection line on the last node
                node.SetConnectionLineVisible(i < _currentSector.Levels.Length - 1);

                _nodes[i] = node;
            }
        }

        void ClearNodes()
        {
            if (_nodes != null)
            {
                foreach (var node in _nodes)
                {
                    if (node)
                    {
                        node.OnClicked -= OnLevelClicked;
                        Destroy(node.gameObject);
                    }
                }
            }

            _nodes = null;
        }

        void RefreshAll()
        {
            if (_currentSector == null || _progression == null) return;

            if (_sectorNameText)
                _sectorNameText.text = _currentSector.DisplayName;

            if (_sectorStarsText)
            {
                int stars = _progression.GetSectorStars(_currentSector.SectorId);
                int maxStars = _currentSector.Levels.Length * 3;
                _sectorStarsText.text = $"★ {stars}/{maxStars}";
            }

            RefreshNodes();
            ScrollToCurrentLevel();
        }

        void RefreshNodes()
        {
            if (_nodes == null || _currentSector?.Levels == null) return;

            for (int i = 0; i < _nodes.Length && i < _currentSector.Levels.Length; i++)
            {
                var level = _currentSector.Levels[i];
                bool unlocked = _progression.IsLevelUnlocked(level.LevelId);
                bool completed = _progression.IsLevelCompleted(level.LevelId);
                int bestStars = _progression.GetBestStars(level.LevelId);

                var state = completed
                    ? LevelNodeState.Completed
                    : unlocked
                        ? LevelNodeState.Available
                        : LevelNodeState.Locked;

                _nodes[i].Setup(level, i + 1, state, bestStars);

                // Connection line color reflects the destination node state
                if (i < _nodes.Length - 1)
                {
                    Color lineColor = state == LevelNodeState.Locked
                        ? new Color(1f, 1f, 1f, 0.15f)
                        : _currentSector.AccentColor;
                    _nodes[i].SetConnectionLineColor(lineColor);
                }
            }
        }

        void ScrollToCurrentLevel()
        {
            if (_scrollRect == null || _nodes == null || _nodes.Length == 0) return;

            // Find the first available (not yet completed) level to scroll to
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].State == LevelNodeState.Available)
                {
                    float normalizedPos = 1f - (float)i / Mathf.Max(1, _nodes.Length - 1);
                    _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
                    return;
                }
            }
        }

        void OnLevelClicked(LevelData level)
        {
            if (_onLevelSelected)
                _onLevelSelected.Raise(level);
        }

        void OnBackClicked()
        {
            _uiService.HideScreen<SectorScreen>();
        }

        void OnDestroy()
        {
            ClearNodes();

            if (_backButton)
                _backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
