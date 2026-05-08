using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class HubScreen : UIScreen
    {
        [Header("Sector Nodes")]
        [SerializeField] SectorNodeWidget[] _sectorNodes;
        [SerializeField] SectorData[] _sectors;

        [Header("Top Bar")]
        [SerializeField] TMP_Text _totalStarsText;
        [SerializeField] FragmentsDisplay _fragmentsDisplay;
        [SerializeField] LivesDisplay _livesDisplay;

        [Header("Ghost")]
        [SerializeField] Transform _ghostTransform;
        [SerializeField] Transform[] _sectorAnchors;

        [Header("Buttons")]
        [SerializeField] Button _shopButton;
        [SerializeField] Button _settingsButton;

        [Header("Connections")]
        [Tooltip("Parent RectTransform under which the bezier connection " +
                 "GameObjects are spawned. Usually the same parent that holds " +
                 "the sector nodes (e.g. MapArea). Leave null to fall back to " +
                 "the first sector node's parent.")]
        [SerializeField] RectTransform _connectionContainer;
        [Tooltip("Bezier line thickness in canvas units.")]
        [SerializeField] float _connectionThickness = 8f;
        [Tooltip("Perpendicular curve offset of the bezier control point. " +
                 "Sign alternates per connection so the path snakes between " +
                 "sectors instead of bending uniformly.")]
        [SerializeField] float _connectionCurve = 90f;

        [Header("Events")]
        [SerializeField] GameEvent<SectorData> _onSectorUnlocked;
        [SerializeField] GameEvent<SectorData> _onSectorCompleted;

        const string IntroShownPrefix = "CutsceneIntroShown:";
        const string OutroShownPrefix = "CutsceneOutroShown:";

        IProgressionService _progression;
        IEconomyService _economy;
        ILivesService _lives;
        IUIService _uiService;
        INotificationService _notifications;

        // Outro cutscenes are queued here when _onSectorCompleted fires
        // (often during play in the Level scene); we drain the queue on the
        // next HubScreen.Show so the cutscene appears once the player is back
        // looking at the Hub.
        readonly Queue<SectorData> _pendingOutros = new();

        // Bezier-line connections between consecutive sector nodes.
        // Replaces the per-node connection Image (which couldn't follow
        // arbitrary designer-placed positions). Rebuilt on Start, recoloured
        // on RefreshAll.
        readonly List<BezierUILine> _connections = new();

        void Start()
        {
            CacheServices();
            BindSectorNodes();
            BindButtons();
            BuildConnections();
            RefreshAll();
        }

        void OnEnable()
        {
            if (_onSectorUnlocked)
                _onSectorUnlocked.AddListener(OnSectorStateChanged);
            if (_onSectorCompleted)
                _onSectorCompleted.AddListener(OnSectorStateChanged);
        }

        void OnDisable()
        {
            if (_onSectorUnlocked)
                _onSectorUnlocked.RemoveListener(OnSectorStateChanged);
            if (_onSectorCompleted)
                _onSectorCompleted.RemoveListener(OnSectorStateChanged);
        }

        void CacheServices()
        {
            _progression = ServiceLocator.Get<IProgressionService>();
            _economy = ServiceLocator.Get<IEconomyService>();
            _lives = ServiceLocator.Get<ILivesService>();
            _uiService = ServiceLocator.Get<IUIService>();

            if (ServiceLocator.Contains<INotificationService>())
            {
                _notifications = ServiceLocator.Get<INotificationService>();
                _notifications.OnChanged += OnNotificationsChanged;
            }
        }

        void OnNotificationsChanged()
        {
            // Rebuild sector badges + the lives badge.
            RefreshSectorNodes();
            RefreshTopBar();
        }

        void BindSectorNodes()
        {
            for (int i = 0; i < _sectorNodes.Length; i++)
            {
                if (_sectorNodes[i])
                    _sectorNodes[i].OnClicked += OnSectorClicked;
            }
        }

        void BindButtons()
        {
            if (_shopButton)
                _shopButton.onClick.AddListener(OnShopClicked);
            if (_settingsButton)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        public override void Show()
        {
            base.Show();
            RefreshAll();
            QueuePendingOutrosFromState();
            DrainPendingOutros();
        }

        /// <summary>
        /// Scan the sector roster and queue outros for any Completed sector
        /// whose outro hasn't been shown yet. The event-driven path
        /// (OnSectorStateChanged) misses cases where _onSectorCompleted fires
        /// while the HubScreen GameObject is inactive — Level loading calls
        /// SetHubUIActive(false), which detaches our listener exactly when
        /// ProgressionService raises the completion event mid-Level.
        /// </summary>
        void QueuePendingOutrosFromState()
        {
            if (_progression == null || _sectors == null) return;

            foreach (var sector in _sectors)
            {
                if (sector == null) continue;
                if (sector.OutroCutscene == null) continue;
                if (!_progression.IsSectorCompleted(sector.SectorId)) continue;
                if (HasOutroBeenShown(sector.SectorId)) continue;
                if (_pendingOutros.Contains(sector)) continue;

                _pendingOutros.Enqueue(sector);
            }
        }

        void RefreshAll()
        {
            if (_progression == null) return;

            RefreshSectorNodes();
            RefreshConnections();
            RefreshTopBar();
            UpdateGhostPosition();
        }

        void RefreshSectorNodes()
        {
            for (int i = 0; i < _sectorNodes.Length && i < _sectors.Length; i++)
            {
                var sector = _sectors[i];
                var state = _progression.GetSectorState(sector.SectorId);
                int stars = _progression.GetSectorStars(sector.SectorId);
                CountSectorProgress(sector, out int completed, out int total);

                _sectorNodes[i].Setup(sector, state, stars, completed, total);

                bool showBadge = _notifications != null
                                 && _notifications.HasNewContent(sector.SectorId);
                _sectorNodes[i].SetBadge(showBadge);
            }
        }

        void CountSectorProgress(SectorData sector, out int completed, out int total)
        {
            completed = 0;
            total = 0;
            if (sector?.Levels == null) return;

            total = sector.Levels.Length;
            for (int i = 0; i < sector.Levels.Length; i++)
            {
                var level = sector.Levels[i];
                if (level != null && _progression.IsLevelCompleted(level.LevelId))
                    completed++;
            }
        }

        // =========================================================================
        // Bezier connections between sector nodes
        // =========================================================================

        void BuildConnections()
        {
            ClearConnections();

            if (_sectorNodes == null || _sectorNodes.Length < 2) return;

            var container = ResolveConnectionContainer();
            if (container == null) return;

            for (int i = 0; i < _sectorNodes.Length - 1; i++)
            {
                var a = _sectorNodes[i];
                var b = _sectorNodes[i + 1];
                if (a == null || b == null) continue;

                var go = new GameObject(
                    $"SectorConnection_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierUILine));
                go.transform.SetParent(container, worldPositionStays: false);

                var crt = (RectTransform)go.transform;
                // Match the level-path setup: anchor at parent centre with
                // zero size, then push start/end points in directly via
                // anchoredPosition coords. Sector nodes' anchored positions
                // are designer-set within this container, so converting via
                // InverseTransformPoint keeps the bezier in the same space.
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = Vector2.zero;
                crt.localScale = Vector3.one;

                var line = go.GetComponent<BezierUILine>();
                line.Thickness = _connectionThickness;
                // Hub uses a uniform bend direction (unlike the level path's
                // zigzag) — sectors are spaced freely by designers, so an
                // alternating curve looks chaotic instead of snaking.
                line.CurveOffset = _connectionCurve;
                // Anchor on each node's LineAnchorTransform — a designer-
                // placed empty child (SectorLineAnchor) marking exactly
                // where the curve should meet the node's silhouette.
                line.SetEndpoints(LocalPos(a.LineAnchorTransform, container),
                                  LocalPos(b.LineAnchorTransform, container));

                // Render behind the sector nodes so the bezier reads as a
                // background path, not an overlay.
                go.transform.SetAsFirstSibling();
                _connections.Add(line);
            }
        }

        void RefreshConnections()
        {
            if (_connections.Count == 0) return;

            for (int i = 0; i < _connections.Count; i++)
            {
                int destIndex = i + 1;
                if (destIndex >= _sectors.Length) continue;

                var destState = _progression.GetSectorState(_sectors[destIndex].SectorId);
                Color c = destState == SectorState.Locked
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : _sectors[destIndex].AccentColor;
                _connections[i].color = c;
            }
        }

        void ClearConnections()
        {
            foreach (var conn in _connections)
                if (conn) Destroy(conn.gameObject);
            _connections.Clear();
        }

        RectTransform ResolveConnectionContainer()
        {
            if (_connectionContainer != null) return _connectionContainer;
            // Fall back to the parent of the first sector node — works for
            // the common case where all nodes share a single MapArea.
            for (int i = 0; i < _sectorNodes.Length; i++)
            {
                if (_sectorNodes[i] == null) continue;
                return _sectorNodes[i].transform.parent as RectTransform;
            }
            return null;
        }

        static Vector2 LocalPos(Transform node, RectTransform container)
        {
            // World→container-local so the bezier sits in the same coord
            // space as the sector nodes' anchored positions.
            return container.InverseTransformPoint(node.position);
        }

        void RefreshTopBar()
        {
            // Glyph (★) lives in a sibling Image now — text holds just the number.
            if (_totalStarsText)
                _totalStarsText.text = _progression.GetTotalStars().ToString();

            if (_fragmentsDisplay)
                _fragmentsDisplay.SetFragments(_economy.GetFragments());

            if (_livesDisplay)
            {
                _livesDisplay.SetLives(_lives.GetCurrentLives());

                bool livesBadge = _notifications != null && _notifications.HasUnclaimedRewards();
                _livesDisplay.SetBadge(livesBadge);
            }
        }

        void UpdateGhostPosition()
        {
            if (!_ghostTransform || _sectorAnchors == null || _sectorAnchors.Length == 0)
                return;

            int targetIndex = FindCurrentSectorIndex();

            if (targetIndex < _sectorAnchors.Length && _sectorAnchors[targetIndex])
                _ghostTransform.position = _sectorAnchors[targetIndex].position;
        }

        int FindCurrentSectorIndex()
        {
            int best = 0;

            for (int i = 0; i < _sectors.Length; i++)
            {
                var state = _progression.GetSectorState(_sectors[i].SectorId);

                if (state == SectorState.InProgress)
                    return i;

                if (state == SectorState.Available || state == SectorState.Completed)
                    best = i;
            }

            return best;
        }

        void OnSectorClicked(SectorData sector)
        {
            // Alpha-test gate: only the first sector ships in this build. Tap-
            // ping any other sector pops a "content unavailable" notice with
            // the current app version. Remove or expand the whitelist when
            // sectors 2..5 ship.
            if (!IsSectorShipped(sector))
            {
                Debug.Log($"[HubScreen] Sector '{sector.SectorId}' not shipped in this build — showing ContentErrorPopup.");
                _uiService?.ShowPopup<ContentErrorPopup>(null);
                return;
            }

            _notifications?.MarkSeen(INotificationService.SectorUnlockId(sector.SectorId));
            // Player is starting a level — they've implicitly acknowledged the
            // "lives are full again" notification.
            _notifications?.MarkSeen(INotificationService.LivesRefilledId);

            // Intro cutscene only on the very first time the player taps into
            // a sector. We mark the flag from the popup's onComplete (not
            // before Show) so the cutscene gets another chance if the popup
            // bailed early — e.g. data was empty, popup wasn't found, etc.
            if (sector.IntroCutscene != null && !HasIntroBeenShown(sector.SectorId))
            {
                ShowCutscene(sector.IntroCutscene, () =>
                {
                    MarkIntroShown(sector.SectorId);
                    OpenSectorScreen(sector);
                });
                return;
            }

            OpenSectorScreen(sector);
        }

        void OnSectorStateChanged(SectorData sector)
        {
            RefreshAll();

            // The completion event can fire mid-Level (ProgressionService runs
            // there). Queue the outro so DrainPendingOutros picks it up the
            // next time the player is looking at the Hub.
            if (sector != null
                && sector.OutroCutscene != null
                && _progression != null
                && _progression.IsSectorCompleted(sector.SectorId)
                && !HasOutroBeenShown(sector.SectorId))
            {
                _pendingOutros.Enqueue(sector);
            }
        }

        // =========================================================================
        // Cutscene plumbing
        // =========================================================================

        void OpenSectorScreen(SectorData sector)
        {
            var sectorScreen = _uiService.GetScreen<SectorScreen>();
            if (sectorScreen == null) return;

            // Populate sector data BEFORE the zoom-in tween runs, so the
            // SectorScreen content is visible from the first frame of the
            // animation rather than popping in halfway through.
            sectorScreen.SetSector(sector);

            var transition = ServiceLocator.Contains<IHubSectorTransition>()
                ? ServiceLocator.Get<IHubSectorTransition>()
                : null;

            if (transition == null)
            {
                _uiService.ShowScreen<SectorScreen>();
                return;
            }

            var focusNode = GetSectorNodeRect(sector);
            transition.ZoomIn(this, sectorScreen, focusNode, onComplete: () =>
            {
                // Hand off to the screen stack so back-button HideScreen
                // pops correctly. Skip the default cover-fade since the
                // zoom already covered the swap.
                _uiService.ShowScreen<SectorScreen>(useTransition: false);
            });
        }

        public RectTransform GetSectorNodeRect(SectorData sector)
        {
            if (_sectors == null || _sectorNodes == null) return null;
            int idx = System.Array.IndexOf(_sectors, sector);
            if (idx < 0 || idx >= _sectorNodes.Length) return null;
            var node = _sectorNodes[idx];
            return node != null ? node.transform as RectTransform : null;
        }

        void DrainPendingOutros()
        {
            if (_pendingOutros.Count == 0) return;

            var sector = _pendingOutros.Dequeue();
            if (sector == null || sector.OutroCutscene == null) return;
            if (HasOutroBeenShown(sector.SectorId)) return;

            // Same as the intro path: mark from the popup's onComplete so a
            // failed/empty cutscene doesn't permanently mark itself as seen.
            ShowCutscene(sector.OutroCutscene, () =>
            {
                MarkOutroShown(sector.SectorId);
                // Keep draining sequentially after the cutscene closes, in
                // case multiple sectors were completed back-to-back.
                DrainPendingOutros();
            });
        }

        void ShowCutscene(CutsceneData data, System.Action onComplete)
        {
            if (_uiService == null || data == null)
            {
                onComplete?.Invoke();
                return;
            }

            var popup = _uiService.GetPopup<CutscenePopup>();
            if (popup == null)
            {
                Debug.LogWarning($"[HubScreen] CutscenePopup not found; skipping '{data.CutsceneId}'.");
                onComplete?.Invoke();
                return;
            }

            popup.Show(data, onComplete);
        }

        static bool HasIntroBeenShown(string sectorId) =>
            false && PlayerPrefs.GetInt(IntroShownPrefix + sectorId, 0) == 1;

        static void MarkIntroShown(string sectorId)
        {
            PlayerPrefs.SetInt(IntroShownPrefix + sectorId, 1);
            PlayerPrefs.Save();
        }

        static bool HasOutroBeenShown(string sectorId) =>
            false && PlayerPrefs.GetInt(OutroShownPrefix + sectorId, 0) == 1;

        static void MarkOutroShown(string sectorId)
        {
            PlayerPrefs.SetInt(OutroShownPrefix + sectorId, 1);
            PlayerPrefs.Save();
        }

        void OnShopClicked()
        {
            _uiService?.ShowScreen<ShopScreen>();
        }

        void OnSettingsClicked()
        {
            _uiService?.ShowPopup<SettingsPopup>(null);
        }

        void OnDestroy()
        {
            ClearConnections();

            if (_sectorNodes != null)
            {
                foreach (var node in _sectorNodes)
                {
                    if (node)
                        node.OnClicked -= OnSectorClicked;
                }
            }

            if (_shopButton)
                _shopButton.onClick.RemoveAllListeners();
            if (_settingsButton)
                _settingsButton.onClick.RemoveAllListeners();

            if (_notifications != null)
                _notifications.OnChanged -= OnNotificationsChanged;
        }

        /// <summary>
        /// Whitelist of sectors that ship in the current alpha build. Returns
        /// true for the first two sectors — taps on sectors 3..5 still show
        /// the ContentErrorPopup. Expand once those are content-complete.
        /// </summary>
        static bool IsSectorShipped(SectorData sector)
        {
            return sector != null && sector.SectorIndex <= 1;
        }
    }
}
