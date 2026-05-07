using System.Collections.Generic;
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
        [SerializeField] RectTransform _levelContainer;
        [SerializeField] LevelNodeWidget _levelNodePrefab;

        [Header("Layout")]
        [Tooltip("Margin from the top of Content to the first node centre.")]
        [SerializeField] float _topMargin = 220f;
        [Tooltip("Margin from the last node to the bottom of Content (gives the " +
                 "scroll some padding).")]
        [SerializeField] float _bottomMargin = 220f;
        [Tooltip("Vertical spacing between consecutive node centres.")]
        [SerializeField] float _stepY = 260f;
        [Tooltip("Base horizontal offset for the alternating zigzag side.")]
        [SerializeField] float _horizontalSpread = 180f;
        [Tooltip("Maximum extra random horizontal jitter applied to each node.")]
        [SerializeField] float _horizontalJitter = 70f;
        [Tooltip("Maximum extra random vertical jitter applied to each node.")]
        [SerializeField] float _verticalJitter = 40f;
        [Tooltip("Seed mixed with sector id so layout is the same every run for a sector.")]
        [SerializeField] int _layoutSeed = 1337;

        [Header("Connections")]
        [Tooltip("Bezier line thickness in canvas units.")]
        [SerializeField] float _connectionThickness = 8f;
        [Tooltip("Bezier control-point perpendicular offset. Alternates sign per " +
                 "connection so the path sways naturally.")]
        [SerializeField] float _connectionCurve = 90f;

        [Header("Navigation")]
        [SerializeField] Button _backButton;

        [Header("Events")]
        [SerializeField] GameEvent<LevelData> _onLevelSelected;
        [Tooltip("Fires when the player finishes a level. Refresh on this so " +
                 "newly-unlocked next levels and updated star counts appear " +
                 "without the player having to back out and re-enter.")]
        [SerializeField] LevelResultEvent _onLevelCompleted;

        SectorData _currentSector;
        LevelNodeWidget[] _nodes;
        readonly List<BezierUILine> _connections = new();
        readonly List<Vector2> _nodePositions = new();

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

        void OnEnable()
        {
            if (_onLevelCompleted) _onLevelCompleted.AddListener(OnLevelCompletedRaised);
        }

        void OnDisable()
        {
            if (_onLevelCompleted) _onLevelCompleted.RemoveListener(OnLevelCompletedRaised);
        }

        void OnLevelCompletedRaised(LevelResult _)
        {
            // ProgressionService has already applied the result by this point
            // (LevelController calls CompleteLevel before raising the event).
            // Re-pull state so the just-finished level shows its star count
            // and the next level transitions out of Locked.
            if (_currentSector != null)
                RefreshAll();
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

        // =========================================================================
        // Layout
        // =========================================================================

        void BuildLevelNodes()
        {
            ClearNodes();
            ClearConnections();

            if (_currentSector?.Levels == null || _levelNodePrefab == null) return;
            if (_levelContainer == null) return;

            // Manual positioning — the legacy VerticalLayoutGroup / SizeFitter
            // would fight us, so disable them if they're still on Content.
            DisableContainerLayout();

            int n = _currentSector.Levels.Length;
            _nodes = new LevelNodeWidget[n];
            _nodePositions.Clear();

            // Deterministic per-sector RNG so layout is consistent across runs
            // — same sector id always produces the same jitter pattern.
            var rng = new System.Random(
                _layoutSeed ^ (_currentSector.SectorId?.GetHashCode() ?? 0));

            for (int i = 0; i < n; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float xJitter = ((float)rng.NextDouble() * 2f - 1f) * _horizontalJitter;
                float yJitter = ((float)rng.NextDouble() * 2f - 1f) * _verticalJitter;

                float x = side * _horizontalSpread + xJitter;
                float y = -(_topMargin + i * _stepY + yJitter);
                _nodePositions.Add(new Vector2(x, y));

                var node = Instantiate(_levelNodePrefab, _levelContainer);
                var rt = node.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(x, y);
                }

                node.OnClicked += OnLevelClicked;
                // The per-node connection Image is replaced by the bezier
                // mesh below. Keep the prefab field around in case you ever
                // want to revert; it just stays hidden.
                node.SetConnectionLineVisible(false);

                _nodes[i] = node;
            }

            // Resize Content so the ScrollRect knows the path height.
            float contentHeight = _topMargin + (n > 0 ? (n - 1) : 0) * _stepY
                                  + _bottomMargin + _verticalJitter;
            var size = _levelContainer.sizeDelta;
            size.y = contentHeight;
            _levelContainer.sizeDelta = size;

            BuildConnections();
        }

        void DisableContainerLayout()
        {
            var layout = _levelContainer.GetComponent<VerticalLayoutGroup>();
            if (layout) layout.enabled = false;
            var hLayout = _levelContainer.GetComponent<HorizontalLayoutGroup>();
            if (hLayout) hLayout.enabled = false;
            var fitter = _levelContainer.GetComponent<ContentSizeFitter>();
            if (fitter) fitter.enabled = false;
        }

        void BuildConnections()
        {
            if (_nodePositions.Count < 2) return;

            for (int i = 0; i < _nodePositions.Count - 1; i++)
            {
                var go = new GameObject(
                    $"Connection_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierUILine));
                go.transform.SetParent(_levelContainer, worldPositionStays: false);

                // Anchor at top-center so the BezierUILine's local space matches
                // the level node anchored coords (start/end positions are pushed
                // straight in, no extra transform math).
                var crt = (RectTransform)go.transform;
                crt.anchorMin = new Vector2(0.5f, 1f);
                crt.anchorMax = new Vector2(0.5f, 1f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = Vector2.zero;
                crt.localScale = Vector3.one;

                var line = go.GetComponent<BezierUILine>();
                line.Thickness = _connectionThickness;
                // Alternate bend direction so the curves snake instead of all
                // bending the same way.
                line.CurveOffset = (i % 2 == 0 ? 1f : -1f) * _connectionCurve;
                line.SetEndpoints(_nodePositions[i], _nodePositions[i + 1]);
                line.color = _currentSector.AccentColor;

                // Render behind the level nodes.
                go.transform.SetAsFirstSibling();
                _connections.Add(line);
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

        void ClearConnections()
        {
            foreach (var conn in _connections)
                if (conn) Destroy(conn.gameObject);
            _connections.Clear();
        }

        // =========================================================================
        // Refresh
        // =========================================================================

        void RefreshAll()
        {
            if (_currentSector == null || _progression == null) return;

            if (_sectorNameText)
                _sectorNameText.text = _currentSector.DisplayName;

            if (_sectorStarsText)
            {
                int stars = _progression.GetSectorStars(_currentSector.SectorId);
                int maxStars = _currentSector.Levels.Length * 3;
                // Glyph (★) lives in a sibling Image now.
                _sectorStarsText.text = $"{stars}/{maxStars}";
            }

            RefreshNodes();
            RefreshConnections();
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
            }
        }

        void RefreshConnections()
        {
            if (_connections.Count == 0 || _currentSector?.Levels == null) return;

            // Connection i links nodes i → i+1. Color reflects the
            // *destination* node's state — locked = ghosted, otherwise sector accent.
            for (int i = 0; i < _connections.Count; i++)
            {
                int destIndex = i + 1;
                if (destIndex >= _currentSector.Levels.Length) continue;

                bool destUnlocked = _progression.IsLevelUnlocked(
                    _currentSector.Levels[destIndex].LevelId);

                Color c = destUnlocked
                    ? _currentSector.AccentColor
                    : new Color(1f, 1f, 1f, 0.15f);
                _connections[i].color = c;
            }
        }

        void ScrollToCurrentLevel()
        {
            if (_scrollRect == null || _nodes == null || _nodes.Length == 0) return;

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

        // =========================================================================

        void OnLevelClicked(LevelData level)
        {
            // If a LevelEntryTransition is registered, dive the camera into
            // the tapped node before raising the event — the LevelLauncher
            // listening to the event then loads the scene under cover of
            // the now-opaque transition overlay.
            var transition = ServiceLocator.Contains<ILevelEntryTransition>()
                ? ServiceLocator.Get<ILevelEntryTransition>()
                : null;

            if (transition == null)
            {
                RaiseLevelSelected(level);
                return;
            }

            var nodePos = GetNodeWorldPosition(level);
            transition.ZoomIn(nodePos, () => RaiseLevelSelected(level));
        }

        void RaiseLevelSelected(LevelData level)
        {
            // Stash the parent sector so Level-scene code (e.g. the result
            // screen's constellation preview) can resolve which sector this
            // level belongs to without a per-level back-pointer.
            SectorData.ActiveSector = _currentSector;

            if (_onLevelSelected)
                _onLevelSelected.Raise(level);
        }

        Vector3 GetNodeWorldPosition(LevelData level)
        {
            if (_nodes != null && _currentSector?.Levels != null)
            {
                int idx = System.Array.IndexOf(_currentSector.Levels, level);
                if (idx >= 0 && idx < _nodes.Length && _nodes[idx] != null)
                    return _nodes[idx].transform.position;
            }
            // Fallback: zoom toward the screen's centre. The zoom still
            // reads as "diving in", just without the directional cue.
            return Camera.main != null
                ? Camera.main.transform.position
                : Vector3.zero;
        }

        void OnBackClicked()
        {
            // Reverse the Hub→Sector zoom on the way out so the entry/exit
            // feel symmetric. HubScreen.Show (called by HideScreen at the
            // end of the tween) is the same trigger that drains pending
            // outro cutscenes today — we keep that invariant by only
            // calling HideScreen after the zoom-out completes.
            var transition = ServiceLocator.Contains<IHubSectorTransition>()
                ? ServiceLocator.Get<IHubSectorTransition>()
                : null;
            var hubScreen = _uiService?.GetScreen<HubScreen>();

            if (transition == null || hubScreen == null)
            {
                _uiService?.HideScreen<SectorScreen>();
                return;
            }

            transition.ZoomOut(this, hubScreen, focusNode: null, onComplete: () =>
            {
                _uiService.HideScreen<SectorScreen>();
            });
        }

        void OnDestroy()
        {
            ClearNodes();
            ClearConnections();

            if (_backButton)
                _backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
