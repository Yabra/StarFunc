using System.Threading.Tasks;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    public class BootInitializer : MonoBehaviour
    {
        const float MinBootDurationSeconds = 3f;

        [Header("Config")]
        [SerializeField] BalanceConfig _balanceConfig;
        [SerializeField] AudioConfig _audioConfig;
        [SerializeField] VfxConfig _vfxConfig;
        [SerializeField] SectorData[] _sectors;

        [Header("Events — Economy")]
        [SerializeField] IntGameEvent _onFragmentsChanged;

        [Header("Events — Lives")]
        [SerializeField] IntGameEvent _onLivesChanged;

        [Header("Events — Progression")]
        [SerializeField] SectorDataEvent _onSectorUnlocked;
        [SerializeField] SectorDataEvent _onSectorCompleted;

        void Awake()
        {
            var sfmObject = new GameObject("[SceneFlowManager]");
            DontDestroyOnLoad(sfmObject);
            var sceneFlowManager = sfmObject.AddComponent<SceneFlowManager>();
            ServiceLocator.Register<SceneFlowManager>(sceneFlowManager);
        }

        async void Start()
        {
            float bootStartedAt = Time.realtimeSinceStartup;

            // LoadingOverlay registers itself on Awake; show it now so the
            // Boot scene has visible UI throughout the async setup. Progress
            // is driven at each milestone below.
            var overlay = ServiceLocator.Contains<ILoadingOverlay>()
                ? ServiceLocator.Get<ILoadingOverlay>()
                : null;
            overlay?.Show();
            overlay?.SetProgress(0.05f);

            // §10.5 step 1 — NetworkMonitor
            var networkObject = new GameObject("[NetworkMonitor]");
            DontDestroyOnLoad(networkObject);
            var networkMonitor = networkObject.AddComponent<NetworkMonitor>();

            // §10.5 step 2 — AuthService (register / refresh token)
            var tokenManager = new TokenManager();
            var apiClient = new ApiClient(tokenManager, networkMonitor);
            var authService = new AuthService(apiClient, tokenManager, networkMonitor);

            ServiceLocator.Register(networkMonitor);
            ServiceLocator.Register(tokenManager);
            ServiceLocator.Register(apiClient);
            ServiceLocator.Register(authService);

            await authService.InitializeAsync();
            overlay?.SetProgress(0.2f);

            // §10.5 step 3 — SaveService
            var saveService = new LocalSaveService();
            ServiceLocator.Register<ISaveService>(saveService);

            // §10.5 — SyncQueue & SyncProcessor (task 2.15)
            var syncQueue = new SyncQueue();
            ServiceLocator.Register(syncQueue);

            var cloudSaveClient = new CloudSaveClient(apiClient);
            ServiceLocator.Register(cloudSaveClient);

            var syncProcessor = new SyncProcessor(
                syncQueue, apiClient, authService, cloudSaveClient, networkMonitor, saveService);
            ServiceLocator.Register(syncProcessor);

            // §10.5 step 6 — ContentService (remote config, bundled fallback)
            var contentService = new ContentService(apiClient, networkMonitor, _balanceConfig);
            ServiceLocator.Register(contentService);
            await contentService.InitializeAsync();
            overlay?.SetProgress(0.6f);

            // §10.5 step 4 — EconomyService (needed before ProgressionService)
            var economyService = new LocalEconomyService(
                saveService, _balanceConfig, _onFragmentsChanged);
            ServiceLocator.Register<IEconomyService>(economyService);

            // §10.5 step 5 — ProgressionService
            var progressionService = new ProgressionService(
                saveService, economyService, _balanceConfig, _sectors,
                _onSectorUnlocked, _onSectorCompleted);
            ServiceLocator.Register<IProgressionService>(progressionService);

            // §10.5 step 6 — LivesService
            var livesService = new LocalLivesService(
                saveService, _balanceConfig, economyService, _onLivesChanged);
            ServiceLocator.Register<ILivesService>(livesService);

            // §10.5 step 7 — TimerService
            var timerService = new TimerService();
            ServiceLocator.Register<ITimerService>(timerService);

            // §10.5 step 7b — AudioService (Music + SFX, DontDestroyOnLoad host)
            var audioObj = new GameObject("[AudioSystem]");
            DontDestroyOnLoad(audioObj);
            var musicPlayer = audioObj.AddComponent<MusicPlayer>();
            var sfxPlayer = audioObj.AddComponent<SFXPlayer>();
            var audioService = new AudioService(musicPlayer, sfxPlayer);
            ServiceLocator.Register<IAudioService>(audioService);

            // §10.5 step 8 — FeedbackService (uses AudioService + AudioConfig for SFX,
            // VfxConfig for ParticleSystem one-shots — task 4.6)
            var feedbackService = new FeedbackService(audioService, _audioConfig, _vfxConfig);
            ServiceLocator.Register<IFeedbackService>(feedbackService);

            // §10.5 step 9 — NotificationService (hub badges)
            var notificationService = new NotificationService(
                saveService, progressionService, _sectors,
                _onSectorUnlocked, _onLivesChanged);
            ServiceLocator.Register<INotificationService>(notificationService);

            // §10.5 step 10 — ShopService: HybridShopService composes a local
            // backing store with a REST surface (task 4.3a). Online purchases
            // are server-authoritative; offline purchases run locally and
            // queue with cachedPrice for SyncProcessor to flush on reconnect.
            var localShopService = new LocalShopService(contentService, economyService, saveService);
            var serverShopService = new ServerShopService(apiClient);
            var shopService = new HybridShopService(
                localShopService, serverShopService,
                networkMonitor, contentService, syncQueue, economyService);
            ServiceLocator.Register<IShopService>(shopService);
            overlay?.SetProgress(0.85f);

            // §10.5 step 11 — AnalyticsService (task 4.8 / 4.8a)
            // REST sender + on-disk batching queue. Initialize() spawns the
            // DontDestroyOnLoad host that drives the 30s flush cadence and
            // emits session_start.
            var analyticsSender = new AnalyticsSender(apiClient, networkMonitor);
            var analyticsService = new AnalyticsService(analyticsSender, networkMonitor);
            ServiceLocator.Register<IAnalyticsService>(analyticsService);
            analyticsService.Initialize();

            // UIService registers itself via MonoBehaviour.Awake in the target scene

            overlay?.SetProgress(1f);

            // Hold the Boot scene visible for at least MinBootDurationSeconds
            // so the splash isn't a single-frame flash on fast machines.
            float elapsed = Time.realtimeSinceStartup - bootStartedAt;
            float remaining = MinBootDurationSeconds - elapsed;
            if (remaining > 0f)
                await Task.Delay(Mathf.CeilToInt(remaining * 1000f));

            // Boot complete — load Hub. SceneFlowManager handles the
            // transition + overlay hide on the other side.
            var sfm = ServiceLocator.Get<SceneFlowManager>();
            sfm.LoadScene("Hub");
        }
    }
}
