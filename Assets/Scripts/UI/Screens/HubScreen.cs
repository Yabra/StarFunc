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

        void Start()
        {
            CacheServices();
            BindSectorNodes();
            BindButtons();
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
            DrainPendingOutros();
        }

        void RefreshAll()
        {
            if (_progression == null) return;

            RefreshSectorNodes();
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

                _sectorNodes[i].Setup(sector, state, stars);

                // Connection line color matches destination sector state
                Color lineColor = state == SectorState.Locked
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : sector.AccentColor;
                _sectorNodes[i].SetConnectionLineColor(lineColor);

                bool showBadge = _notifications != null
                                 && _notifications.HasNewContent(sector.SectorId);
                _sectorNodes[i].SetBadge(showBadge);
            }
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
            _uiService.ShowScreen<SectorScreen>();

            var sectorScreen = _uiService.GetScreen<SectorScreen>();
            if (sectorScreen)
                sectorScreen.SetSector(sector);
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
    }
}
