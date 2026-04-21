using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    public class BootInitializer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] BalanceConfig _balanceConfig;
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

            // §10.5 step 8 — FeedbackService
            var feedbackService = new FeedbackService();
            ServiceLocator.Register<IFeedbackService>(feedbackService);

            // UIService registers itself via MonoBehaviour.Awake in the target scene

            // Boot complete — load Hub
            var sfm = ServiceLocator.Get<SceneFlowManager>();
            sfm.LoadScene("Hub");
        }
    }
}
