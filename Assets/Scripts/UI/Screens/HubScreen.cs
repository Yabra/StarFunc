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

        IProgressionService _progression;
        IEconomyService _economy;
        ILivesService _lives;
        IUIService _uiService;

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
            }
        }

        void RefreshTopBar()
        {
            if (_totalStarsText)
                _totalStarsText.text = $"★ {_progression.GetTotalStars()}";

            if (_fragmentsDisplay)
                _fragmentsDisplay.SetFragments(_economy.GetFragments());

            if (_livesDisplay)
                _livesDisplay.SetLives(_lives.GetCurrentLives());
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
            _uiService.ShowScreen<SectorScreen>();

            var sectorScreen = _uiService.GetScreen<SectorScreen>();
            if (sectorScreen)
                sectorScreen.SetSector(sector);
        }

        void OnSectorStateChanged(SectorData _)
        {
            RefreshAll();
        }

        void OnShopClicked()
        {
            // Stub — ShopScreen will be implemented in Phase 3
        }

        void OnSettingsClicked()
        {
            // Stub — SettingsScreen will be implemented in Phase 3
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
        }
    }
}
