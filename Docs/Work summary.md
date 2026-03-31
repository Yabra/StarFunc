# Отчёт о проделанной работе — STAR FUNC

---

## Содержание

1. [Общая сводка](#общая-сводка)
2. [Клиентская часть — Unity](#клиентская-часть--unity)
   - [Фаза 0 — Подготовка проекта](#фаза-0--подготовка-проекта-done)
   - [Фаза 1 — Ядро и прототип](#фаза-1--ядро-и-прототип-done)
   - [Фаза 2 — Основные системы](#фаза-2--основные-системы-не-начата)
   - [Фазы 3–4 — Не начаты](#фазы-34--не-начаты)
3. [Серверная часть — Backend](#серверная-часть--backend)
   - [Фаза S0 — Инфраструктура](#фаза-s0--инфраструктура-done)
   - [Фаза S1 — Аутентификация](#фаза-s1--аутентификация-done)
   - [Фаза S2 — Основные сервисы](#фаза-s2--основные-сервисы-done)
   - [Фаза S3 — Ключевая бизнес-логика](#фаза-s3--ключевая-бизнес-логика-не-начата)
   - [Фаза S4 — Магазин, аналитика](#фаза-s4--магазин-аналитика-частично)
4. [Тестовое покрытие](#тестовое-покрытие)
5. [Текущие проблемы и замечания](#текущие-проблемы-и-замечания)
6. [Что предстоит сделать](#что-предстоит-сделать)

---

## Общая сводка

| Сторона | Фаз всего     | Завершено      | В процессе / частично | Не начато   |
| ------- | ------------- | -------------- | --------------------- | ----------- |
| Клиент  | 5 фаз (0–4)   | 2 (0, 1)       | —                     | 3 (2, 3, 4) |
| Бэкенд  | 5 фаз (S0–S4) | 3 (S0, S1, S2) | 1 (S4 частично)       | 1 (S3)      |

**Unity-скриптов написано:** 76 `.cs` файлов (~3 700 строк только по ключевым файлам), охватывающих Фазы 0 и 1 полностью.

**Backend-файлов написано:** 60 `.py` файлов (~1 200 строк по сервисам и роутерам), охватывающих Фазы S0, S1, S2 полностью и часть S4.

**Тесты:** 116 тестов (116 проходят, 0 падает).

**Игровой контент:** 5 секторов × 20 уровней = **100 уровней** созданы и заполнены в seed-данных.

---

## Клиентская часть — Unity

### Технические параметры проекта

| Параметр          | Значение                       |
| ----------------- | ------------------------------ |
| Движок            | Unity 6 (6000.4.0f1)           |
| Render Pipeline   | URP 17.4.0, 2D Renderer        |
| Платформа         | Android (portrait, min API 24) |
| Input System      | Unity Input System 1.19.0      |
| UI                | uGUI 2.0.0                     |
| Scripting Backend | IL2CPP, ARM64                  |
| JSON-сериализация | Newtonsoft.Json                |

### Assembly Definitions (asmdef)

Код разбит на 6 сборок, что обеспечивает изолированную компиляцию и чёткие зависимости:

| Сборка                    | Зависимости | Назначение                                          |
| ------------------------- | ----------- | --------------------------------------------------- |
| `StarFunc.Core`           | Unity       | Ядро: события, сервис-локатор, цвета                |
| `StarFunc.Data`           | Core        | Модели данных, enum, конфиги, SO                    |
| `StarFunc.Gameplay`       | Core, Data  | Геймплей: уровень, звёзды, плоскость                |
| `StarFunc.Input`          | Gameplay    | Обработка ввода (Input System)                      |
| `StarFunc.UI`             | Core, Data  | UI-экраны, виджеты, попапы                          |
| `StarFunc.Infrastructure` | Core, Data  | Сеть, сохранения, сцены, Boot                       |
| `StarFunc.Meta`           | Core, Data  | Прогрессия, экономика, жизни (папки пусты — Фаза 2) |

---

### Фаза 0 — Подготовка проекта (Done)

#### Задача 0.1 — Структура папок и Assembly Definitions ✅

Создана полная иерархия каталогов внутри `Assets/`:

```txt
Scripts/
├── Core/
├── Data/
│   ├── Configs/
│   ├── Enums/
│   ├── Runtime/
│   └── ScriptableObjects/
├── Gameplay/
│   ├── CoordinatePlane/
│   ├── FunctionEditor/
│   ├── Ghost/
│   ├── Graph/
│   ├── Level/
│   └── Stars/
├── Infrastructure/
│   ├── Analytics/
│   ├── Auth/
│   ├── Boot/
│   ├── Network/
│   ├── Save/
│   └── Scenes/
├── Input/
├── Meta/
│   ├── Audio/
│   ├── Economy/
│   ├── Feedback/
│   ├── Lives/
│   ├── Notifications/
│   ├── Progression/
│   ├── Shop/
│   └── Timer/
└── UI/
    ├── Base/
    ├── Overlays/
    ├── Popups/
    ├── Screens/
    ├── Service/
    └── Widgets/
```

Созданы все `.asmdef` файлы. В пустые папки добавлены `.gitkeep`.

#### Задача 0.2 — Сцены и базовая конфигурация ✅

- Созданы три сцены: `Boot.unity`, `Hub.unity`, `Level.unity`.
- Сцены зарегистрированы в Build Settings: Boot → 0, Hub → 1, Level → 2.
- Player Settings: Portrait, IL2CPP, ARM64, Android API 24+.
- Установлен и настроен TextMesh Pro, импортированы TMP Essential Resources.

#### Задача 0.3 — Настройка URP ✅

- Создан URP Asset с 2D Renderer в `Assets/Settings/`.
- Настроен профиль качества «Mobile».
- HDR отключён, MSAA отключён, Depth/Opaque Texture отключены для максимальной производительности на мобильных.
- Bloom с минимальной интенсивностью для glow-эффектов звёзд.

---

### Фаза 1 — Ядро и прототип (Done)

#### Задача 1.1 — ServiceLocator и событийная система ✅

**`Core/ServiceLocator.cs`** — статический generic-класс:

- `Register<T>()`, `Get<T>()`, `Contains<T>()`, `Reset()`
- Хранит сервисы в `Dictionary<Type, object>`
- Выбрасывает исключение при попытке получить незарегистрированный сервис

**`Core/GameEvent.cs`** — ScriptableObject-событие без параметров:

- `Raise()`, `RegisterListener()`, `UnregisterListener()`
- Хранит список `GameEventListener`

**`Core/GameEventGeneric.cs`** — `GameEvent<T> : ScriptableObject`:

- Обобщённое событие с параметром типа `T`
- Работает с любым сериализуемым типом

**`Core/GameEventListener.cs`** и **`Core/GameEventListenerGeneric.cs`** — MonoBehaviour-слушатели:

- Регистрация в `OnEnable`, отписка в `OnDisable`
- Вызывают `UnityEvent` / `UnityEvent<T>`

**`Core/ColorTokens.cs`** — статический класс с дизайн-токенами:

- `BG_DARK`, `BG_SECOND`, `LINE_PRIMARY`, `POINT_PRIMARY`, `ACCENT_PINK`, `UI_NEUTRAL`, `ERROR`, `SUCCESS`

#### Задача 1.2 — Перечисления и конфигурационные структуры ✅

**Enum-ы** (папка `Data/Enums/`):

| Файл              | Значения                                                                                          |
| ----------------- | ------------------------------------------------------------------------------------------------- |
| `LevelType.cs`    | Tutorial, Normal, Bonus, Control, Final                                                           |
| `TaskType.cs`     | ChooseCoordinate, ChooseFunction, AdjustGraph, BuildFunction, IdentifyError, RestoreConstellation |
| `StarState.cs`    | Hidden, Active, Placed, Incorrect, Restored                                                       |
| `SectorState.cs`  | Locked, Available, InProgress, Completed                                                          |
| `FunctionType.cs` | Linear, Quadratic, Sinusoidal, Mixed                                                              |
| `HintTrigger.cs`  | OnLevelStart, AfterErrors, OnFirstInteraction                                                     |
| `GhostEmotion.cs` | Idle, Happy, Sad, Excited, Determined                                                             |
| `FeedbackType.cs` | StarPlaced, StarError, LevelComplete, ConstellationRestored, ButtonTap, SectorUnlock              |
| `AnswerType.cs`   | ChooseOption, Function, IdentifyStars, PlaceStars                                                 |

**Конфигурационные структуры** (`Data/Configs/`):

- `StarConfig` — `[Serializable]`: `StarId`, `Coordinate (Vector2)`, `InitialState`, `IsControlPoint`, `IsDistractor`, `BelongsToSolution`, `RevealAfterAction`
- `StarRatingConfig` — пороги ошибок для 3/2/1 звёзд, `TimerAffectsRating`, `ThreeStarMaxTime`
- `HintConfig` — `Trigger`, `HintText`, `HighlightPosition`, `TriggerAfterErrors`
- `GraphVisibilityConfig` — `PartialReveal`, `InitialVisibleSegments`, `RevealPerCorrectAction`
- `CutsceneFrame` — `Background`, `CharacterSprite`, `Emotion`, `Text`, `Duration`, `FrameAnimation`
- `AnswerOption` — `OptionId`, `Text`, `Value`, `IsCorrect`
- `ShopItem` — `ItemId`, категория, цена, описание

#### Задача 1.3 — ScriptableObject-определения данных ✅

**`Data/ScriptableObjects/SectorData.cs`** (`[CreateAssetMenu(menuName = "StarFunc/Data/SectorData")]`):

- `SectorId`, `DisplayName`, `SectorIndex`, `Levels[]`, `PreviousSector`, `RequiredStarsToUnlock`
- Визуальные поля: `AccentColor`, `StarColor`, `BackgroundSprite`
- `IntroCutscene`, `OutroCutscene` (ссылки на `CutsceneData`)

**`Data/ScriptableObjects/LevelData.cs`**:

- `LevelId`, `LevelIndex`, `Type (LevelType)`, `TaskType`
- Координатная плоскость: `PlaneMin`, `PlaneMax`, `GridStep`
- `Stars[]`, `ReferenceFunctions[]`, `AnswerOptions[]`
- `AccuracyThreshold`, `StarRating (StarRatingConfig)`
- `MaxAttempts`, `MaxAdjustments`
- Memory Mode: `UseMemoryMode`, `MemoryDisplayDuration`
- `GraphVisibility (GraphVisibilityConfig)`, `Hints[]`, `ShowHints`
- `FragmentReward`
- Статический `ActiveLevel` — глобальный слот для передачи между сценами

**`Data/ScriptableObjects/FunctionDefinition.cs`**:

- `Type (FunctionType)`, `Coefficients[]`, `DomainRange`

**`Data/ScriptableObjects/CutsceneData.cs`**:

- `CutsceneId`, `Frames[] (CutsceneFrame[])`

#### Задача 1.4 — Runtime-модели данных ✅

**`Data/Runtime/PlayerSaveData.cs`** — полная модель сохранения игрока:

- Версионирование: `SaveVersion`, `Version` (optimistic lock), `LastModified`
- Прогрессия: `Dictionary<string, SectorProgress>`, `Dictionary<string, LevelProgress>`, `CurrentSectorIndex`
- Экономика: `TotalFragments`
- Жизни: `CurrentLives`, `LastLifeRestoreTimestamp`
- Магазин: `OwnedItems (List<string>)`, `Consumables (Dictionary<string, int>)`
- Статистика: `TotalLevelsCompleted`, `TotalStarsCollected`, `TotalPlayTime`
- Метод `IncrementVersion()` — атомарно обновляет счётчик и `LastModified`

**`Data/Runtime/PlayerAnswer.cs`** — поддерживает 4 типа ответов (API.md §5.5):

- `ChooseOption`: `SelectedOptionId`
- `Function`: `FunctionType`, `Coefficients[]`
- `IdentifyStars`: `SelectedStarIds (List<string>)`
- `PlaceStars`: `Placements (List<StarPlacement>)`
- Устаревшее поле `SelectedCoordinate` для локальной валидации

**`Data/Runtime/ValidationResult.cs`** — результат валидации (API.md §5.6):

- `IsValid`, `Stars`, `FragmentsEarned`, `Time`, `ErrorCount`, `MatchPercentage`, `Errors[]`

Остальные runtime-модели: `SectorProgress`, `LevelProgress`, `LevelResult`, `PlayerAction`, `FunctionParams`, `AnswerData`, `StarData`, `PopupData`, `StarPlacement`.

#### Задача 1.5 — SceneFlowManager и BootInitializer ✅

**`Infrastructure/Scenes/SceneFlowManager.cs`** — DontDestroyOnLoad:

- `LoadLevel(LevelData)` — аддитивная загрузка Level-сцены, скрывает Hub UI
- `UnloadLevel()` — выгрузка Level-сцены, восстанавливает Hub UI
- `LoadScene(string)` — полная замена сцены (Boot → Hub)
- Отображает `LoadingOverlay` во время переходов

**`UI/Overlays/LoadingOverlay.cs`** — наследник `UIScreen`:

- Полноэкранное затемнение с текстом «Loading...»
- Реализует интерфейс `ILoadingOverlay` (зарегистрирован в ServiceLocator)

**`Infrastructure/Boot/BootInitializer.cs`** — точка входа приложения:

- `Awake()`: создаёт `SceneFlowManager` (DontDestroyOnLoad), регистрирует в ServiceLocator
- `Start()`: асинхронная инициализация сети (шаги по API.md §10.5):
  1. `NetworkMonitor` — инициализация и мониторинг сети
  2. `TokenManager` — управление JWT-токенами
  3. `ApiClient` — HTTP-клиент
  4. `AuthService.InitializeAsync()` — анонимная регистрация / обновление токена
  5. Переход в Hub-сцену через `SceneFlowManager.LoadScene("Hub")`
- Последующие шаги 3–11 по §10.5 оставлены для Фазы 2 (LocalSaveService, HybridSaveService и т.д.)

#### Задача 1.6 — CoordinatePlane ✅

**`Gameplay/CoordinatePlane/CoordinatePlane.cs`**:

- Публичные свойства: `PlaneMin`, `PlaneMax`, `GridStep`
- `WorldToPlane(Vector2)`, `PlaneToWorld(Vector2)` — преобразование координат
- `Initialize(planeMin, planeMax, gridStep)` — настройка из `LevelData`
- Автоматически инициализирует дочерние компоненты

**`Gameplay/CoordinatePlane/GridRenderer.cs`**:

- Отрисовка сетки через `LineRenderer`-ы
- Цвет линий: `ColorTokens.BG_SECOND`
- Динамическое обновление при изменении параметров

**`Gameplay/CoordinatePlane/AxisRenderer.cs`** — оси X и Y с выделенным цветом, толще сетки.

**`Gameplay/CoordinatePlane/CoordinateLabeler.cs`**:

- TextMeshPro-метки вдоль осей с шагом `GridStep`
- `Rebuild()` — пересоздание при изменении параметров

**`Gameplay/CoordinatePlane/TouchInputHandler.cs`**:

- **Использует Unity Input System 1.19.0** (не legacy `UnityEngine.Input`)
- Tap → raycasting → `WorldToPlane()` → событие `OnPlaneClicked(Vector2)`

**`Gameplay/CoordinatePlane/PlaneCamera.cs`**:

- Pinch-to-zoom для мобильных, scroll для редактора
- Ограничение области видимости в пределах `PlaneMin`–`PlaneMax`

#### Задача 1.7 — StarEntity ✅

**`Gameplay/Stars/StarEntity.cs`**:

- `Initialize(StarConfig)`, `SetState(StarState)`, `GetCoordinate()`
- Делегирует визуал, анимации, взаимодействие дочерним компонентам
- Событие `OnTapped` — пробрасывает из `StarInteraction`

**`Gameplay/Stars/StarVisuals.cs`**:

- SpriteRenderer + glow-спрайт (аддитивный материал)
- Цвет по состоянию: Active → `POINT_PRIMARY`, Placed → `SUCCESS`, Incorrect → `ERROR`

**`Gameplay/Stars/StarAnimator.cs`** — анимации через Coroutine:

- `PlayAppear()` — fade-in (alpha 0→1) + scale (0.5→1.0)
- `PlayPlace()` — белый flash + glow pulse
- `PlayError()` — shake (±0.05 ед.) + красный flash
- `PlayRestore()` — заглушка

**`Gameplay/Stars/StarInteraction.cs`** — `IPointerClickHandler` или Input System:

- Событие `OnStarTapped`
- `SetInteractable(bool)` — включение/выключение взаимодействия

**`Gameplay/Stars/StarManager.cs`**:

- `SpawnStars(StarConfig[])` — инстанцирование из префаба по конфигу
- `GetStar(string starId)`, `GetAllPlaced()`, `ResetAll()`
- Хранит `Dictionary<string, StarEntity>`

#### Задача 1.8 — GhostEntity ✅

**`Gameplay/Ghost/GhostEntity.cs`** — главный MonoBehaviour персонажа:

- `SetEmotion(GhostEmotion)` — делегирует в `GhostVisuals` и `GhostAnimator`

**`Gameplay/Ghost/GhostVisuals.cs`**:

- SpriteRenderer + glow-эффект
- Смена спрайта по эмоции (placeholder-спрайт, цвет меняется)

**`Gameplay/Ghost/GhostAnimator.cs`**:

- Idle-анимация через `Mathf.Sin` (плавное покачивание)
- Эмоциональные переходы

**`Gameplay/Ghost/GhostEmotionController.cs`**:

- Подписывается на SO-события: `OnStarCollected → Happy`, `OnStarRejected → Sad`, `OnLevelCompleted → Excited`
- Вызывает `GhostEntity.SetEmotion()`

**`Gameplay/Ghost/GhostPositioner.cs`** — фиксированное позиционирование рядом с координатной плоскостью.

#### Задача 1.9 — LevelController и AnswerSystem ✅

**`Gameplay/Level/LevelController.cs`** (431 строка) — центральный автомат уровня:

Состояния: `None → Initialize → ShowTask → MemoryPreview → AwaitInput → ValidateAnswer → CalculateResult → ShowResult → Complete / Failed`

Реализованные механики:

- **Memory Mode**: если `LevelData.UseMemoryMode == true` → показывает эталон на `MemoryDisplayDuration` секунд, затем скрывает
- **GraphVisibility**: поддержка `PartialReveal`, управление `_visibleSegments`
- **MaxAttempts**: при `attempt >= MaxAttempts` → `failReason = "max_attempts_reached"`, переход в `Failed`
- **Жизни**: проверка `ILivesService.HasLives()` при входе — при 0 жизней вход заблокирован (показывает `NoLivesPopup`)
- **Списание жизни**: один раз за попытку (при `FailAttempt()`, а не за каждую локальную ошибку)
- **Провал**: `failReason: "no_lives"` при `currentLives == 0` после списания
- **SO-события**: `_onLevelStarted`, `_onLevelCompleted`, `_onLevelFailed`, `_onStarCollected`, `_onStarRejected`, `_onAnswerConfirmed`
- **Передача LevelData**: через статический `LevelData.ActiveLevel`, с фолбэком на inspector-поле для тестирования

**`Gameplay/Level/AnswerSystem.cs`** (151 строка) — реализован режим `ChooseCoordinate`:

- Принимает `LevelData` и генерирует `AnswerOption[]` в панели
- `GetCurrentAnswer() : PlayerAnswer` — возвращает выбранный вариант
- Вызывает SO-событие `OnAnswerSelected(AnswerData)` при выборе

#### Задача 1.10 — ValidationSystem и LevelResultCalculator ✅

**`Gameplay/Level/ValidationSystem.cs`** (131 строка):

- `ValidateCoordinate(Vector2, Vector2, float)` — расстояние с порогом
- `ValidateFunction(FunctionDefinition, FunctionDefinition, float)` — заглушка (Phase 2)
- `ValidateControlPoints(StarConfig[], StarConfig[])` — сравнение по `StarId` и координатам, возвращает `ValidationResult` с `MatchPercentage`
- `ValidateLevel(LevelData, PlayerAnswer) : LevelResult` — диспетчер по `TaskType`

**`Gameplay/Level/LevelResultCalculator.cs`** (142 строки):

- `Calculate(LevelData, int errors, float time) : LevelResult`
- Определяет 0–3 звезды по порогам из `StarRatingConfig`
- Учитывает `TimerAffectsRating` и `ThreeStarMaxTime`
- Рассчитывает `FragmentsEarned` (базовая награда + бонус за улучшение)
- При повторном прохождении: `bestStars = max(old, new)`, `bestTime = min(old, new)`

**`Gameplay/Level/ActionHistory.cs`** (34 строки):

- Стек действий: `Push(PlayerAction)`, `Undo() : PlayerAction`, `Reset()`, `CanUndo : bool`

**`Gameplay/Level/LevelTimer.cs`** (67 строк):

- `Time.realtimeSinceStartup`-based
- `Start()`, `Stop()`, `Pause()`, `Resume()`, `GetElapsedTime()`

#### Задача 1.11 — Минимальный LevelHUD ✅

**`UI/Base/UIScreen.cs`** — абстрактный базовый класс:

- `Show()`, `Hide()`, `IsVisible`
- `CanvasGroup` для fade-анимации

**`UI/Base/UIPopup.cs`** — абстрактный базовый класс попапа:

- `Show(PopupData)`, `Hide()`
- Затемнение фона

**`UI/Screens/LevelHUD.cs`** — наследник `UIScreen`:

- `TimerDisplay`, `LivesDisplay`, `AnswerPanel`
- Кнопки: Pause, Confirm, Undo, Reset, Hint (Hint пока неактивна)
- Связан с `LevelController` и `ActionHistory`

**`UI/Widgets/TimerDisplay.cs`** — TextMeshPro, обновляется каждый кадр через `LevelTimer`.

**`UI/Widgets/LivesDisplay.cs`** — отображение числа жизней.

**`UI/Widgets/AnswerPanel.cs`** — генерирует кнопки-варианты из `AnswerOption[]`.

**`UI/Overlays/LoadingOverlay.cs`** — полноэкранное затемнение с текстом «Loading...», реализует `ILoadingOverlay`.

#### Задача 1.12 — ApiClient, NetworkMonitor, TokenManager ✅

**`Infrastructure/Network/ApiClient.cs`** (386 строк) — обёртка над `UnityWebRequest`:

- `Get<T>(endpoint)`, `Post<T>(endpoint, body)`, `Put<T>(endpoint, body)` — async Task<ApiResult\<T>>
- Автоматические заголовки: `Authorization: Bearer <token>`, `Content-Type: application/json`, `Accept-Encoding: gzip`, `X-Client-Version`, `X-Platform`
- **Idempotency-Key** (UUID) автоматически для POST/PUT запросов (API.md §9.5)
- Десериализация `{ status, data, serverTime }` и `{ status, error: { code, message, details } }`
- **Retry-стратегия** (API.md §4.3):
  - `401` → refresh token → повтор
  - `429` → экспоненциальная задержка (1с, 2с, 4с), макс. 3 попытки
  - `500/503` → задержка, макс. 3 → offline
  - Timeout → 1 retry → offline
- Hard timeout 10 сек, soft timeout 5 сек
- gzip-декодирование ответов

**`Infrastructure/Network/ApiEndpoints.cs`** — все константы URL:

- `BaseUrl = "https://api.starfunc.app/api/v1"`
- 18 эндпоинтов из API.md §6: `/auth/register`, `/auth/refresh`, `/auth/link`, `/save`, `/economy/balance`, `/economy/transaction`, `/lives`, `/lives/restore`, `/lives/restore-all`, `/shop/items`, `/shop/purchase`, `/content/manifest`, `/content/sectors`, `/content/levels/{id}`, `/content/balance`, `/check/level`, `/analytics/events`, `/health`

**`Infrastructure/Network/NetworkMonitor.cs`** (71 строка):

- Свойство `IsOnline : bool`
- Событие `OnConnectivityChanged(bool isOnline)`
- Периодическая проверка + `Application.internetReachability`

**`Infrastructure/Network/TokenManager.cs`** (174 строки):

- Access Token хранится в памяти (не на диске), TTL 1 час
- Refresh Token зашифрован AES-256 в `persistentDataPath` (ключ = deviceId + hardware fingerprint)
- `GetAccessToken()`, `GetRefreshToken()`, `SetTokens()`, `ClearTokens()`
- Метод `RefreshAccessToken()` вызывает `POST /auth/refresh` и ротирует refresh token

#### Задача 1.13 — AuthService (клиентский) ✅

**`Infrastructure/Auth/AuthService.cs`** (395 строк) — полная реализация:

- `deviceId` — UUID v7 (или v4 как фолбэк), зашифрован AES-256 в `persistentDataPath`
- `playerId` — также зашифрован на диске
- `InitializeAsync()` — boot-time entry point:
  - Offline → пропуск, работа в offline-режиме
  - Есть refresh token → `RefreshToken()`, при неудаче → `Register()`
  - Первый запуск → `Register()`
- `Register()` — `POST /auth/register` с `{ deviceId, platform, clientVersion }`. Идемпотентно (возвращает существующего игрока по deviceId)
- `RefreshToken()` — `POST /auth/refresh`, при `401` → offline-режим
- `LinkAccount(provider, providerToken)` — `POST /auth/link`, обработка `409 ACCOUNT_ALREADY_LINKED`
- `IsAuthenticated`, `PlayerId`, `DeviceId` — публичные свойства

---

### Фаза 2 — Основные системы (Не начата)

Все директории созданы, `.gitkeep` размещены, но **ни один C#-класс не реализован**.

Ожидается реализация следующих задач:

| Задача | Что нужно                                                                                           | Статус |
| ------ | --------------------------------------------------------------------------------------------------- | ------ |
| 2.1    | `ISaveService`, `LocalSaveService` (JSON + SHA-256 checksum)                                        | ❌     |
| 2.1a   | `CloudSaveClient`, `SaveMerger`, `HybridSaveService`                                                | ❌     |
| 2.2    | `IProgressionService`, `ProgressionService`                                                         | ❌     |
| 2.3    | `IEconomyService`, `LocalEconomyService`                                                            | ❌     |
| 2.3a   | `ServerEconomyService`, `HybridEconomyService`                                                      | ❌     |
| 2.4    | `ILivesService`, `LocalLivesService`                                                                | ❌     |
| 2.4a   | `ServerLivesService`, `HybridLivesService`                                                          | ❌     |
| 2.5    | `ITimerService`, `TimerService`, `IFeedbackService`, `FeedbackService`                              | ❌     |
| 2.6    | `IUIService`, `UIService`, `LevelResultScreen`, `StarRatingDisplay`, `FragmentsDisplay`             | ❌     |
| 2.7    | `HubScreen` — карта галактики                                                                       | ❌     |
| 2.8    | `SectorScreen` — карта уровней                                                                      | ❌     |
| 2.9    | `GraphRenderer`, `FunctionEvaluator`, `CurveRenderer`, `ControlPointsRenderer`, `ComparisonOverlay` | ❌     |
| 2.10   | `AnswerSystem`: режим `ChooseFunction`                                                              | ❌     |
| 2.11   | 18 SO-ассетов событий в `ScriptableObjects/Events/`                                                 | ❌     |
| 2.12   | Обновление `BootInitializer` — регистрация всех сервисов                                            | ❌     |
| 2.13   | `ContentService` (RemoteConfig + bundled JSON fallback)                                             | ❌     |
| 2.14   | `LevelCheckClient`, `ReconciliationHandler` (POST /check/level)                                     | ❌     |
| 2.15   | `SyncQueue`, `SyncProcessor` — offline-first инфраструктура                                         | ❌     |

> **Замечание**: `ILivesService` и `ILoadingOverlay` вынесены в `Core/` как интерфейсы — это нестандартное место (по плану они должны быть в `Meta/Lives/` и `UI/Overlays/`). Это технически работает, но нарушает изначальную структуру.

---

### Фазы 3–4 — Не начаты

Никакие файлы из Фаз 3 и 4 не созданы. Ожидаются:

- Фаза 3: `FunctionEditor`, полный `GraphRenderer`, все 6 типов `TaskType`, `HintSystem`, анимации, попапы, `NotificationService`, `LoadingOverlay` (полная версия), SO-ассеты 100 уровней
- Фаза 4: `AudioService`, `CutscenePopup`, `LocalShopService`/`HybridShopService`, `SettingsPopup`, VFX, оптимизация, `AnalyticsService`, безопасность (certificate pinning), финальное тестирование

---

## Серверная часть — Backend

### Технические параметры

| Параметр        | Значение                                         |
| --------------- | ------------------------------------------------ |
| Язык            | Python 3.12                                      |
| Фреймворк       | FastAPI >= 0.110                                 |
| ASGI-сервер     | Uvicorn + Gunicorn                               |
| БД              | PostgreSQL 16 (asyncpg + SQLAlchemy 2.0 async)   |
| Кеш             | Redis 7 (hiredis)                                |
| Аутентификация  | JWT (python-jose, HS256)                         |
| Валидация       | Pydantic v2.7                                    |
| Логирование     | structlog (JSON в production)                    |
| Метрики         | prometheus-fastapi-instrumentator                |
| ORM             | SQLAlchemy 2.0 async                             |
| Миграции        | Alembic 1.13                                     |
| Контейнеризация | Docker (multi-stage build, python:3.12-slim, uv) |

### Структура проекта (Backend/)

```txt
Backend/
├── src/app/
│   ├── main.py               # FastAPI factory + lifespan
│   ├── config.py             # Pydantic Settings
│   ├── dependencies.py       # FastAPI Depends()
│   ├── domain/               # Доменные модели и правила
│   │   ├── entities.py
│   │   ├── enums.py
│   │   ├── models.py
│   │   ├── check_models.py   # Заглушка (S3.1+)
│   │   ├── content_models.py
│   │   ├── shop_models.py
│   │   ├── exceptions.py
│   │   └── rules/
│   │       ├── economy.py
│   │       └── lives.py
│   ├── services/             # Бизнес-логика
│   │   ├── auth_service.py
│   │   ├── save_service.py
│   │   ├── economy_service.py
│   │   ├── lives_service.py
│   │   └── content_service.py
│   ├── api/
│   │   ├── routers/          # HTTP-роутеры
│   │   ├── middleware/       # Цепочка middleware
│   │   └── schemas/          # Pydantic-схемы
│   └── infrastructure/
│       ├── database.py
│       ├── redis.py
│       ├── auth/             # JwtProvider, verifiers
│       ├── cache/            # Idempotency, rate limiter, content cache
│       └── persistence/      # ORM-репозитории
├── alembic/                  # Миграции
│   └── versions/cb5a26dbb18c_initial_create_all_tables.py
├── seed/                     # Начальные данные
│   ├── seed_content.py
│   └── data/
│       ├── balance.json
│       ├── sectors.json
│       └── levels/           # sector_1.json ... sector_5.json
├── tests/
│   ├── unit/
│   ├── service/
│   └── integration/          # Заглушки (S4.4)
├── Dockerfile
├── docker-compose.yml
├── docker-compose.override.yml
└── pyproject.toml
```

---

### Фаза S0 — Инфраструктура (Done)

#### Задача S0.1 — Скаффолдинг проекта ✅

**`config.py`** — `Pydantic Settings` с полным набором переменных:

- `database_url`, `redis_url`
- JWT: `jwt_secret`, `jwt_algorithm`, `jwt_access_token_expire_minutes` (60), `jwt_refresh_token_expire_days` (90)
- Game Balance: `max_lives` (5), `restore_interval_seconds` (1800), `restore_cost_fragments` (20), `skip_level_cost_fragments` (100), `improvement_bonus_per_star` (5), `hint_cost_fragments` (10)
- Rate Limits для каждого endpoint-а (auth: 10, save: 30, economy: 60, lives: 30, check: 60, analytics: 10, content: 30, shop: 30)
- `idempotency_key_expiration_hours` (24), `env`, `log_level`, `workers`

**`main.py`** — `create_app()` фабрика FastAPI:

- `lifespan` async context manager: инициализация async engine + Redis при старте, graceful shutdown
- Prometheus: `Instrumentator().instrument(app).expose(app, endpoint="/metrics")`
- Swagger UI (`/docs`), ReDoc (`/redoc`)
- Корректный порядок middleware

**Docker-конфигурация**:

- Multi-stage Dockerfile на `python:3.12-slim` с uv
- `docker-compose.yml`: сервисы `api`, `db` (postgres:16-alpine), `redis` (redis:7-alpine) с healthchecks
- `docker-compose.override.yml`: монтирование томов, порты отладки для разработки

#### Задача S0.2 — БД и миграции ✅

**`infrastructure/persistence/models.py`** — 6 SQLAlchemy ORM-моделей:

| Модель                | Таблица            | Ключевые поля                                                                                                                                   |
| --------------------- | ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `PlayerModel`         | `players`          | `id (UUID PK)`, `device_id (UUID UNIQUE)`, `platform CHECK('android','ios')`, `client_version`, `google_play_id`, `apple_gc_id`, `display_name` |
| `PlayerSaveModel`     | `player_saves`     | `player_id (FK UNIQUE)`, `version (INT)`, `save_version (INT)`, `save_data (JSONB)`                                                             |
| `TransactionModel`    | `transactions`     | `player_id (FK)`, `type CHECK('earn','spend')`, `amount`, `reason`, `reference_id`, `previous_bal`, `new_bal`, `idempotency_key`                |
| `RefreshTokenModel`   | `refresh_tokens`   | `token_hash (UNIQUE)`, `expires_at`, `is_revoked`, `replaced_by_id (self-FK)`                                                                   |
| `ContentVersionModel` | `content_versions` | `content_type`, `content_id`, `version`, `data (JSONB)`, `is_active`                                                                            |
| `AnalyticsEventModel` | `analytics_events` | `player_id`, `session_id`, `event_name`, `params (JSONB)`, `client_ts`, `server_ts`                                                             |

**Алембик** — начальная миграция `cb5a26dbb18c`:

- Все 6 таблиц + 12 индексов (`idx_players_device_id`, `idx_player_saves_player`, `idx_saves_fragments`, `idx_saves_lives`, `idx_transactions_player`, `idx_transactions_idempotency` (partial WHERE idempotency_key IS NOT NULL), `idx_refresh_tokens_player`, `idx_refresh_tokens_hash`, `idx_content_type` (partial WHERE is_active = TRUE), `idx_analytics_player`, `idx_analytics_event`)
- `alembic upgrade head` / `alembic downgrade base` работают корректно

**`infrastructure/database.py`** — async engine factory:

- `create_async_engine(database_url, pool_size=10, max_overflow=20)`
- `async_sessionmaker(engine, expire_on_commit=False)`

#### Задача S0.3 — Redis интеграция ✅

**`infrastructure/redis.py`**:

- `create_redis_pool(redis_url)` → `redis.asyncio.Redis`
- `close_redis_pool(redis)` — graceful shutdown
- Вспомогательные функции: `get_cached()`, `set_cached()`, `delete_cached()`

**`infrastructure/cache/idempotency_store.py`**:

- Ключ: `idempotency:{player_id}:{endpoint}:{key}` (привязан к player_id для безопасности)
- TTL 24 часа
- `get_response()`, `store_response()`

**`infrastructure/cache/rate_limiter.py`**:

- INCR + EXPIRE через Redis
- `check_rate_limit(identifier, endpoint, limit, window=60) -> bool`

**`infrastructure/cache/content_cache.py`**:

- Ключи: `content:manifest` (5 мин), `content:sector:{id}` (10 мин), `content:levels:{id}` (10 мин), `content:balance` (10 мин), `content:shop` (10 мин), `player:balance:{playerId}` (5 мин)
- `get_content()`, `set_content()`, `invalidate_content()`

**`infrastructure/cache/token_store.py`** — заглушка для хранения данных о токенах.

#### Задача S0.4 — CI/CD ✅

Настроены GitHub Actions:

- **CI pipeline**: Python 3.12 + uv → lint (ruff) → type check (mypy) → unit tests → integration tests → Docker build → push в GHCR
- **Deploy pipeline**: триггер на push в `main` → staging (автоматически) → production (manual approval)

---

### Фаза S1 — Аутентификация (Done)

#### Задача S1.1 — JwtProvider ✅

**`infrastructure/auth/jwt_provider.py`**:

- `create_access_token(player_id, platform) -> str`: payload `sub`, `platform`, `iat`, `exp`, `iss: "starfunc-api"`, `aud: "starfunc-client"`, алгоритм HS256
- `create_refresh_token(player_id) -> str`: TTL 90 дней, `type: "refresh"`
- `decode(token) -> dict`: валидация `audience`, `issuer`, `exp`; выбрасывает `jose.JWTError` при невалидном токене
- `hash_token(token) -> str`: SHA-256 хеш refresh-токена (оригинал не хранится на сервере)

**Тесты** (`tests/unit/test_jwt_provider.py`): создание и декодирование, expired tokens, неверная подпись, проверка claims.

#### Задача S1.2 — AuthService + auth router ✅

**`services/auth_service.py`** (206 строк):

`register(request, session)`:

1. Поиск по `device_id` в БД
2. Если найден → выдаёт новые токены (`is_new_player=False`)
3. Если нет → создаёт `Player` + начальный `PlayerSave` с дефолтным `PlayerSaveData` + генерирует токены (`is_new_player=True`)
4. Сохраняет SHA-256 хеш refresh-токена в `refresh_tokens`

`refresh(request, session)`:

1. Хеширование полученного refresh-токена
2. Поиск → проверка `is_revoked == False` и `expires_at > now`
3. Отзыв текущего токена, генерация новой пары
4. **Детекция кражи токена**: если токен уже отозван → отзывает **всю цепочку** для player_id → `401`

**`api/schemas/auth.py`**: `RegisterRequest`, `RefreshRequest`, `AuthResponse`, `LinkAccountRequest`, `LinkResponse`.

**`api/routers/auth.py`**: `POST /auth/register`, `POST /auth/refresh`.

**`dependencies.py`**: `get_current_player()` — извлечение и декодирование Bearer token → `player_id: UUID`.

**`domain/exceptions.py`**: `AppError`, `NotFoundError`, `ConflictError`, `ForbiddenError`, `InsufficientFundsError`, `NoLivesError`.

**Тесты** (`tests/unit/test_auth_service.py`): регистрация нового игрока, идемпотентность, refresh, детекция кражи, expired token.

#### Задача S1.3 — Middleware pipeline ✅

**6 middleware** в `api/middleware/`:

| Middleware                   | Назначение                                                                                                                |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `RequestLoggingMiddleware`   | Structured JSON логирование запроса (метод, путь, player_id, X-Client-Version) и ответа (статус, время) через structlog   |
| `ExceptionHandlerMiddleware` | Глобальный `try-except`: `AppError` → JSON с кодом, `ValidationError` → 422, unhandled → 500                              |
| `RateLimitingMiddleware`     | `/auth/*` → по IP (10 req/min); остальные → по `player_id` с лимитом по эндпоинту; при превышении → `429` с `Retry-After` |
| `ClientInfoMiddleware`       | Извлечение `X-Client-Version` и `X-Platform` в `request.state`                                                            |
| `IdempotencyMiddleware`      | POST/PUT запросы: проверка Idempotency-Key в Redis → кеширование ответа (TTL 24ч)                                         |
| `ServerTimeMiddleware`       | Добавление `serverTime: int` (Unix timestamp) в каждый JSON-ответ                                                         |

Порядок выполнения (Starlette: последний добавленный = самый внешний):
`Request → Logging → ExceptionHandler → RateLimiting → ClientInfo → [Auth via Depends] → Idempotency → [Pydantic] → ServerTime → Router`

**Тесты** (`tests/unit/test_middleware.py`): проверка каждого middleware компонента.

#### Задача S1.4 — Привязка сторонних аккаунтов ✅

**`infrastructure/auth/google_verifier.py`** — `GooglePlayVerifier`:

- Верификация токена через Google Play Games API
- Возвращает `google_play_id`

**`infrastructure/auth/apple_verifier.py`** — `AppleGameCenterVerifier`:

- Верификация подписи через Apple Verification API
- Возвращает `apple_gc_id`

**`services/auth_service.py`** — метод `link_account()`:

1. Верификация через провайдер
2. Проверка уникальности: если `provider_id` уже привязан к другому игроку → `409 ACCOUNT_ALREADY_LINKED`
3. Запись в `players.google_play_id` / `players.apple_gc_id`

**`api/routers/auth.py`**: `POST /auth/link` (authenticated).

**Тесты** (`tests/service/test_link_account.py`).

---

### Фаза S2 — Основные сервисы (Done)

#### Задача S2.1 — SaveService + save router ✅

**`services/save_service.py`** (117 строк):

`get_save(player_id, session)`:

- Загрузка из БД, возврат `save_data + version`
- Если нет записи → дефолтный `PlayerSaveData` с `version=1`

`put_save(player_id, request, session)`:

1. Загрузка текущей записи из БД
2. Если `request.expected_version != current_version` → `409 SAVE_CONFLICT` с серверным сохранением
3. Совпадение → обновление `save_data`, `version = expected_version + 1`, `updated_at = now`

**Оптимистичная блокировка (optimistic lock)** — версионирование через поле `version`.

**`api/schemas/save.py`**: `SaveResponse`, `SaveRequest`, `SaveUpdateResponse`, `SaveConflictResponse`.

**`api/routers/save.py`**: `GET /save`, `PUT /save` (Idempotency-Key required).

**Тесты** (`tests/unit/test_save_service.py`): получение, обновление, конфликт, дефолтное сохранение.

#### Задача S2.2 — EconomyService + economy router ✅

**`services/economy_service.py`** (196 строк):

`get_balance(player_id, session)`:

- Redis-кеш `player:balance:{playerId}` (TTL 5 мин)
- Miss → загрузка из `player_saves.save_data->>'totalFragments'`

`execute_transaction(player_id, request, idempotency_key, session)`:

1. Проверка idempotency → повторный запрос → вернуть кешированный ответ
2. `SELECT ... FOR UPDATE` на `player_saves`
3. `EconomyRules.validate_transaction()` → `422 INSUFFICIENT_FUNDS`
4. Обновление `save_data->'totalFragments'`
5. **`reason == "skip_level"`**: обновление `level_progress` + `sector_progress` + разблокировка
6. Запись в `transactions`
7. Инвалидация Redis-кеша
8. Вся операция в одной SQL-транзакции

**`domain/rules/economy.py`** — `EconomyRules`:

- `calculate_level_reward(level, stars) -> int`
- `calculate_improvement_bonus(old_stars, new_stars, config) -> int` — `(new_stars - old_stars) * improvement_bonus_per_star`, только при `new_stars > old_stars`
- `validate_transaction(type, amount, balance) -> bool`

**Тесты** (`tests/unit/test_economy_rules.py`, `tests/service/test_economy_service.py`):

- Расчёт наград и бонусов, валидация, earn/spend операции, idempotency.

#### Задача S2.3 — LivesService + lives router ✅

**`services/lives_service.py`** (201 строка):

`get_lives(player_id, session)`:

- Пересчёт через `LivesRules.recalculate()` с `server_now = int(datetime.now(UTC).timestamp())`
- Если жизни изменились → обновление `save_data` в БД

`restore_one(player_id, session)`:

1. `SELECT ... FOR UPDATE`, пересчёт жизней
2. Если `current_lives >= max_lives` → `400 LIVES_ALREADY_FULL`
3. Списание `restore_cost_fragments` → `422 INSUFFICIENT_FUNDS`
4. `current_lives += 1`
5. Запись транзакции, обновление `save_data`

`restore_all(player_id, session)`:

- Восстановление до `max_lives`
- Стоимость: `restore_cost_fragments × (max_lives - current_lives)`

**`domain/rules/lives.py`** — `LivesRules.recalculate()`:

```python
elapsed = server_now - last_restore_ts
restored = elapsed // restore_interval_seconds
new_lives = min(current_lives + restored, max_lives)
# Корректный расчёт seconds_until_next и last_restore_timestamp
```

**Тесты** (`tests/unit/test_lives_rules.py`): восстановление после паузы, не превышает максимум, полные жизни, счётчик до следующей жизни.

**`api/routers/lives.py`**: `GET /lives`, `POST /lives/restore`, `POST /lives/restore-all`.

#### Задача S2.4 — ContentService + content router ✅

**`services/content_service.py`** (112 строк) — двухуровневый кеш (Redis → PostgreSQL):

Методы:

- `get_manifest()` — агрегация версий всех типов контента
- `get_sectors()` — все 5 секторов
- `get_sector(sector_id)` — один сектор
- `get_levels(sector_id)` — все уровни сектора
- `get_level(level_id)` — один уровень, `404 LEVEL_NOT_FOUND` если не найден
- `get_balance_config()` — `BalanceConfig`

**`domain/content_models.py`** — полные dataclass-модели контента:
`StarDefinition`, `StarRatingConfig`, `AnswerOption`, `GraphVisibilityConfig`, `HintDefinition`, `ReferenceFunctionDef`, `LevelDefinition`, `SectorDefinition`, `BalanceConfig`, `ContentManifest`

**`infrastructure/persistence/content_repo.py`** — `ContentRepository`:

- `get_active_content()`, `get_all_active_by_type()`, `get_manifest()`

**`api/routers/content.py`**: `GET /content/manifest`, `/sectors`, `/sectors/{id}`, `/sectors/{id}/levels`, `/levels/{id}`, `/balance`.

#### Задача S2.5 — Начальное заполнение контента ✅

**`seed/data/sectors.json`** — 5 секторов:

| Сектор   | Название          | Порог звёзд |
| -------- | ----------------- | ----------- |
| sector_1 | Первые шаги       | 0 (первый)  |
| sector_2 | Линейные функции  | —           |
| sector_3 | Параболы          | —           |
| sector_4 | Синусоиды         | —           |
| sector_5 | Смешанные функции | —           |

Каждый сектор: 20 уровней в порядке: 0 Tutorial, 1–5 Normal, 6 Bonus, 7–10 Normal, 11 Bonus, 12–17 Normal, 18 Control, 19 Final.

**`seed/data/levels/sector_N.json`** — по 20 уровней (100 итого):

| Сектор   | Задания                               |
| -------- | ------------------------------------- |
| sector_1 | Только `ChooseCoordinate` (обучающий) |
| sector_2 | `ChooseCoordinate` + `ChooseFunction` |
| sector_3 | `ChooseCoordinate` + `ChooseFunction` |
| sector_4 | `ChooseCoordinate` + `ChooseFunction` |
| sector_5 | `ChooseCoordinate` + `ChooseFunction` |

Структура каждого уровня: звёзды с `starId`, `coordinate`, `isControlPoint`, `isDistractor`, `belongsToSolution`; `answerOptions[]` с флагом `isCorrect`; `starRatingConfig`; `fragmentReward`.

**`seed/data/balance.json`**:

```json
{
  "maxLives": 5,
  "restoreIntervalSeconds": 1800,
  "restoreCostFragments": 20,
  "skipLevelCostFragments": 100,
  "improvementBonusPerStar": 5,
  "hintCostFragments": 10
}
```

**`seed/data/shop_catalog.json`** — каталог товаров (подсказки-consumable, скины-permanent).

**`seed/seed_content.py`** — скрипт заполнения: upsert в `content_versions` для всех объектов, идемпотентный.

---

### Фаза S3 — Ключевая бизнес-логика (Не начата)

Все задачи Фазы S3 **не реализованы**. Созданы только минимальные заглушки (пустые `check_models.py`).

| Задача | Что нужно                                                                                                        | Статус |
| ------ | ---------------------------------------------------------------------------------------------------------------- | ------ |
| S3.1   | `domain/rules/validation_engine.py` — `ValidationEngine` для all 6 TaskType                                      | ❌     |
| S3.2   | `domain/rules/star_rating.py` — `StarRatingCalculator`                                                           | ❌     |
| S3.3   | `domain/rules/progression.py` — `ProgressionRules` (разблокировка уровней/секторов)                              | ❌     |
| S3.4   | `services/level_check_service.py` — `LevelCheckService` (атомарный `POST /check/level`) + `api/routers/check.py` | ❌     |
| S3.5   | `domain/rules/save_merger.py` — `SaveMerger` (стратегия слияния при 409 SAVE_CONFLICT)                           | ❌     |

Это **самый критичный блок** незавершённой работы на бэкенде, так как `POST /check/level` является центральным эндпоинтом — он атомарно валидирует ответ, обновляет прогрессию, списывает жизни, начисляет фрагменты и возвращает `new_save_version`.

---

### Фаза S4 — Магазин, аналитика, тесты (Частично)

| Задача | Что нужно                                                                    | Статус                                                                   |
| ------ | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| S4.1   | `services/shop_service.py`, `api/routers/shop.py`                            | ❌                                                                       |
| S4.2   | `services/analytics_service.py`, `api/routers/analytics.py`                  | ❌                                                                       |
| S4.3   | `api/routers/health.py` (liveness + readiness), кастомные Prometheus-метрики | ❌ (Prometheus инструментация подключена, но `/health` роутер не создан) |
| S4.4   | `tests/integration/` — интеграционные тесты (testcontainers)                 | ❌ (папка создана, `__init__.py` добавлен — тестов нет)                  |
| S4.5   | Полная OpenAPI-документация                                                  | ⚠️ (базовые метаданные есть, примеры не добавлены)                       |

---

## Тестовое покрытие

### Статистика тестов

```txt
Всего тестов:   116
Проходят:       116
Падают:           0
```

### Покрытые области

| Файл                        | Тесты                                                                          | Статус |
| --------------------------- | ------------------------------------------------------------------------------ | ------ |
| `test_jwt_provider.py`      | Создание/декодирование токенов, expired, неверная подпись, claims, хеширование | ✅     |
| `test_auth_service.py`      | Регистрация (новый/существующий), refresh, детекция кражи, expired             | ✅     |
| `test_economy_rules.py`     | Расчёт награды, бонуса за улучшение, валидация earn/spend                      | ✅     |
| `test_economy_service.py`   | Earn/spend транзакции, insufficient funds, idempotency, skip_level             | ✅     |
| `test_lives_rules.py`       | Пересчёт жизней, не превышает max, полные жизни, seconds_until_next            | ✅     |
| `test_save_service.py`      | Get/Put, конфликт версий, дефолтное сохранение                                 | ✅     |
| `test_content_service.py`   | Получение контента, Redis-кеш, фильтрация по sector_id                         | ✅     |
| `test_middleware.py`        | ExceptionHandler, rate limiter, idempotency                                    | ✅     |
| `test_redis_integration.py` | Redis pool, get/set, rate limiter, idempotency store                           | ✅     |
| `test_placeholder.py`       | Smoke-тест CI pipeline                                                         | ✅     |
| `test_link_account.py`      | Привязка Google/Apple аккаунта, 409 ACCOUNT_ALREADY_LINKED                     | ✅     |

### Отсутствующие тесты

- `domain/rules/validation_engine.py` — нет (S3.1 не реализована)
- `domain/rules/star_rating.py` — нет (S3.2 не реализована)
- `domain/rules/progression.py` — нет (S3.3 не реализована)
- `services/level_check_service.py` — нет (S3.4 не реализована)
- `domain/rules/save_merger.py` — нет (S3.5 не реализована)
- Интеграционные тесты — не реализованы (папка `tests/integration/` пуста)

---

## Текущие проблемы и замечания

### Backend

1. **Отсутствует `POST /check/level`** — самый критичный эндпоинт. Без него клиент не может отправлять результаты уровней на сервер. Это означает, что весь клиент-серверный flow прохождения уровней работает только в offline-режиме.

2. **Нет магазина** (`/shop/items`, `/shop/purchase`) — клиентский `LocalShopService` тоже не реализован.

3. **Нет аналитики** (`/analytics/events`) — события пока не собираются.

4. **Нет health endpoint** — `/health` и `/health/ready` не реализованы, что затрудняет мониторинг в production.

5. **`domain/rules/save_merger.py` отсутствует** — при `409 SAVE_CONFLICT` в `PUT /save` клиент получает серверное сохранение, но не может его корректно слить с локальным.

### Client (Unity)

1. **`ILivesService` и `ILoadingOverlay` в `Core/`** — нестандартное расположение. По архитектуре они должны быть в `Meta/Lives/` и `UI/Overlays/` соответственно. Нарушает SRP принцип asmdef-разбиения.

2. **Вся Фаза 2 не начата** — нет `LocalSaveService`, нет `ProgressionService`, нет `LocalEconomyService`, нет `LocalLivesService`. Это означает, что прогресс игрока **не сохраняется** между сессиями, жизни не работают, фрагменты не накапливаются.

3. **`BootInitializer` не полный** — регистрирует только `NetworkMonitor`, `TokenManager`, `ApiClient`, `AuthService`. После Фазы 2 должен также регистрировать `ISaveService`, `IProgressionService`, `IEconomyService`, `ILivesService`.

4. **SO-события не созданы** — 18 `.asset` файлов событий (`OnStarCollected`, `OnLevelCompleted`, etc.) не созданы в `Assets/ScriptableObjects/Events/`. `GhostEmotionController` и другие компоненты, подписывающиеся на события, не могут быть вручную настроены в инспекторе.

5. **`GraphRenderer` и `FunctionEditor` папки пусты** — уровни с `TaskType = ChooseFunction`, `AdjustGraph` и т.д. не могут быть запущены.

6. **`AnswerSystem` реализует только `ChooseCoordinate`** — 5 из 6 типов заданий не поддерживаются.

7. **`LevelHUD` нет связи с сервисами** — `LivesDisplay` показывает статическое число (нет `LocalLivesService`), `AnswerPanel` не связан с реальным прогрессом игрока.

8. **Нет HubScreen и SectorScreen** — игра не имеет рабочего UI для навигации по секторам и уровням. Переход между уровнями невозможен через UI.

---

## Что предстоит сделать

### Приоритеты (по убыванию важности)

#### Backend (S3 — критично)

1. **S3.2 — `StarRatingCalculator`** — простая бизнес-логика, нет зависимостей. Реализовать первым.
2. **S3.1 — `ValidationEngine`** — валидация ответов для 6 типов заданий. Зависит от seed-данных (готово).
3. **S3.3 — `ProgressionRules`** — разблокировка уровней/секторов. Зависит от seed-данных.
4. **S3.4 — `LevelCheckService`** — центральный атомарный сервис. Зависит от S3.1–S3.3.
5. **S3.5 — `SaveMerger`** — слияние конфликтов для `PUT /save`.

#### Backend (S4 — важно)

1. **S4.3 — Health endpoints** (`/health`, `/health/ready`) — нужны для мониторинга.
2. **S4.1 — ShopService** — магазин с каталогом и покупкой.
3. **S4.2 — AnalyticsService** — приём аналитических событий.
4. **S4.4 — Integration tests** — полные сквозные тесты с testcontainers.

#### Client Unity (Фаза 2 — критично)

1. **2.1 — `LocalSaveService`** — без сохранений игрок теряет прогресс при каждом перезапуске.
2. **2.2 — `ProgressionService`** — без прогрессии нет разблокировок и навигации.
3. **2.3 — `LocalEconomyService`** — баланс фрагментов.
4. **2.4 — `LocalLivesService`** — система жизней с таймером.
5. **2.6 — `UIService` + `LevelResultScreen`** — без экрана результата уровень "зависает".
6. **2.11 — SO-ассеты событий** — создать 18 `.asset` файлов.
7. **2.12 — Обновить `BootInitializer`** — зарегистрировать все сервисы.
8. **2.7/2.8 — `HubScreen`/`SectorScreen`** — навигация по игре.
9. **2.9 — `GraphRenderer`** — поддержка типов заданий с графиками.

#### Client Unity (Фазы 3–4 — следующий этап)

- `FunctionEditor`, полный `AnswerSystem`, `HintSystem`, анимации, попапы, `AudioService`, VFX, оптимизация, контент 100 уровней, безопасность.

---

## Итого

Проект имеет **прочный архитектурный фундамент**: разработана полная система типов данных, событийная архитектура, сетевой и аутентификационный слой на обеих сторонах, а также весь серверный слой хранения и раздачи контента. Реализована **рабочая игровая механика уровня** (координатная плоскость, звёзды, выбор координаты, подсчёт результата).

Основная незавершённость — **промежуточный слой**: клиентские мета-сервисы (прогрессия, сохранения, экономика, жизни) и серверная бизнес-логика (валидация ответов, подсчёт рейтинга, `POST /check/level`). Именно эти компоненты связывают геймплей с progression-петлёй и клиент-серверным взаимодействием.
