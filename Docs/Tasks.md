# План разработки STAR FUNC — Задачи

> Порядок задач выстроен по принципу зависимостей: каждая следующая задача опирается на результаты предыдущих.
> Фазы соответствуют разделу «Порядок реализации» из Architecture.md, но детализированы до уровня отдельных файлов и критериев завершения.
> Все пути указаны относительно `Assets/Scripts/`.

---

## Сводка фаз

| Фаза | Название            | Задач | Что на выходе                                               |
| ---- | ------------------- | ----- | ----------------------------------------------------------- |
| 0    | Подготовка проекта  | 3     | Папки, сцены, настройки URP, asmdef                         |
| 1    | Ядро и прототип     | 11    | Играбельный уровень с выбором координаты, HUD, Ghost        |
| 2    | Основные системы    | 11    | Сохранения, прогрессия, экономика, жизни, хаб, графики      |
| 3    | Расширение механик  | 9     | Все типы заданий, подсказки, анимации, уведомления, контент |
| 4    | Полировка и контент | 10    | Аудио, катсцены, магазин, настройки, оптимизация, аналитика |

---

## ~~Фаза 0 — Подготовка проекта~~ (Done)

### ~~Задача 0.1 — Создание структуры папок и Assembly Definitions~~ (Done)

**Суть:** создать полную иерархию каталогов проекта и assembly definition файлы для разделения кода на модули компиляции.

**Что сделать:**

- Создать дерево папок внутри `Assets/`:
  - `Scripts/Core/`, `Scripts/Data/ScriptableObjects/`, `Scripts/Data/Configs/`, `Scripts/Data/Runtime/`, `Scripts/Data/Enums/`
  - `Scripts/Gameplay/CoordinatePlane/`, `Scripts/Gameplay/Stars/`, `Scripts/Gameplay/Graph/`, `Scripts/Gameplay/Ghost/`, `Scripts/Gameplay/Level/`, `Scripts/Gameplay/FunctionEditor/`
  - `Scripts/Meta/Progression/`, `Scripts/Meta/Economy/`, `Scripts/Meta/Lives/`, `Scripts/Meta/Timer/`, `Scripts/Meta/Shop/`, `Scripts/Meta/Notifications/`, `Scripts/Meta/Audio/`, `Scripts/Meta/Feedback/`
  - `Scripts/UI/Base/`, `Scripts/UI/Service/`, `Scripts/UI/Screens/`, `Scripts/UI/Popups/`, `Scripts/UI/Overlays/`, `Scripts/UI/Widgets/`
  - `Scripts/Infrastructure/Save/`, `Scripts/Infrastructure/Scenes/`, `Scripts/Infrastructure/Analytics/`, `Scripts/Infrastructure/Boot/`
  - `Prefabs/UI/`, `Prefabs/Gameplay/`, `Prefabs/Effects/`
  - `ScriptableObjects/Levels/`, `ScriptableObjects/Sectors/`, `ScriptableObjects/Functions/`, `ScriptableObjects/Config/`, `ScriptableObjects/Events/`
  - `Art/Sprites/`, `Art/Animations/`, `Art/Materials/`, `Art/Shaders/`, `Art/Fonts/`
  - `Audio/Music/`, `Audio/SFX/`
  - `Resources/Localization/`
  - `Plugins/` — нативные плагины / SDK
- Создать `.asmdef` файлы:
  - `StarFunc.Core` — ядро (без зависимостей кроме Unity)
  - `StarFunc.Data` — данные (зависит от Core)
  - `StarFunc.Gameplay` — геймплей (зависит от Core, Data)
  - `StarFunc.Meta` — мета-системы (зависит от Core, Data)
  - `StarFunc.UI` — UI (зависит от Core, Data, Meta)
  - `StarFunc.Infrastructure` — инфраструктура (зависит от Core, Data)
- Добавить `.gitkeep` в пустые папки, которые должны попасть в VCS.

**Зависимости:** нет.

**Критерий завершения:** проект компилируется, все папки видны в Unity Project Window.

---

### ~~Задача 0.2 — Создание сцен и базовая конфигурация проекта~~ (Done)

**Суть:** создать три сцены (Boot, Hub, Level) и настроить Build Settings.

**Что сделать:**

- Создать сцены в `Assets/Scenes/`:
  - `Boot.unity` — пустая сцена, будет содержать `BootInitializer`
  - `Hub.unity` — сцена карты галактики
  - `Level.unity` — универсальная игровая сцена (загружается аддитивно поверх Hub)
- Добавить сцены в Build Settings в правильном порядке (Boot → 0, Hub → 1, Level → 2).
- Настроить Player Settings:
  - Ориентация: Portrait
  - Минимальный API Level: Android 7.0 (API 24)
  - Scripting Backend: IL2CPP
  - Target architectures: ARM64
  - Company Name, Product Name: заполнить

**Зависимости:** 0.1.

- Установить TextMesh Pro через Package Manager (если не установлен) и импортировать TMP Essential Resources (`Window > TextMeshPro > Import TMP Essential Resources`).

**Критерий завершения:** все 3 сцены открываются, Build Settings корректны, Player Settings настроены, TMP Essential Resources импортированы.

---

### ~~Задача 0.3 — Настройка URP для мобильной 2D-игры~~ (Done)

**Суть:** сконфигурировать Universal Render Pipeline под 2D мобильный проект.

**Что сделать:**

- Создать URP Asset в `Assets/Settings/` (если не создан):
  - Renderer: 2D Renderer
  - HDR: отключить (или включить, если нужен bloom/glow — решить и зафиксировать)
  - MSAA: Off или 2x
  - Depth Texture: Off
  - Opaque Texture: Off
- Настроить Quality Settings — один профиль «Mobile» с этим URP Asset.
- Настроить Volume Profile для минимального post-processing (если нужен bloom для glow-эффектов звёзд — добавить Bloom с низкой интенсивностью).

**Зависимости:** 0.2.

**Критерий завершения:** сцена рендерится через 2D Renderer, нет ошибок в консоли.

---

## Фаза 1 — Ядро и прототип

### Задача 1.1 — ServiceLocator и событийная система

**Суть:** реализовать ядро архитектуры — сервис-локатор и ScriptableObject-события.

**Файлы:**

- `Core/ServiceLocator.cs` — статический класс с `Register<T>()`, `Get<T>()`, `Contains<T>()`. Хранит сервисы в `Dictionary<Type, object>`. Добавить метод `Reset()` для очистки при смене сцен/тестах.
- `Core/GameEvent.cs` — ScriptableObject-событие без параметров. Содержит `List<GameEventListener>`, методы `Raise()`, `RegisterListener()`, `UnregisterListener()`.
- `Core/GameEventGeneric.cs` — `GameEvent<T> : ScriptableObject` — обобщённое событие с параметром типа `T`.
- `Core/GameEventListener.cs` — MonoBehaviour, привязывается к `GameEvent` в инспекторе, вызывает `UnityEvent` при `Raise()`. В `OnEnable` регистрируется, в `OnDisable` отписывается.
- `Core/GameEventListenerGeneric.cs` — MonoBehaviour-слушатель для `GameEvent<T>`, вызывает `UnityEvent<T>`.
- `Core/ColorTokens.cs` — статический класс с цветовыми константами (`BG_DARK`, `BG_SECOND`, `LINE_PRIMARY`, `POINT_PRIMARY`, `ACCENT_PINK`, `UI_NEUTRAL`, `ERROR`, `SUCCESS`).

**Зависимости:** 0.1.

**Критерий завершения:** можно зарегистрировать сервис, получить его обратно; SO-событие создаётся через меню, `Raise()` вызывает слушателей.

---

### Задача 1.2 — Перечисления и конфигурационные структуры данных

**Суть:** создать все enum-ы и сериализуемые конфигурационные структуры, от которых зависит остальной код.

**Файлы (Enums):**

- `Data/Enums/LevelType.cs` — `Tutorial, Normal, Bonus, Control, Final`
- `Data/Enums/TaskType.cs` — `ChooseCoordinate, ChooseFunction, AdjustGraph, BuildFunction, IdentifyError, RestoreConstellation`
- `Data/Enums/StarState.cs` — `Hidden, Active, Placed, Incorrect, Restored`
- `Data/Enums/SectorState.cs` — `Locked, Available, InProgress, Completed`
- `Data/Enums/FunctionType.cs` — `Linear, Quadratic, Sinusoidal, Mixed`
- `Data/Enums/HintTrigger.cs` — `OnLevelStart, AfterErrors, OnFirstInteraction`
- `Data/Enums/GhostEmotion.cs` — `Idle, Happy, Sad, Excited, Determined`
- `Data/Enums/FeedbackType.cs` — `StarPlaced, StarError, LevelComplete, ConstellationRestored, ButtonTap, SectorUnlock`

**Файлы (Configs):**

- `Data/Configs/StarConfig.cs` — `[Serializable]`: `StarId`, `Coordinate (Vector2)`, `InitialState`, `IsControlPoint`, `IsDistractor`, `BelongsToSolution`, `RevealAfterAction`
- `Data/Configs/StarRatingConfig.cs` — `[Serializable]`: пороги ошибок для 3/2/1 звёзд, `TimerAffectsRating`, `ThreeStarMaxTime`
- `Data/Configs/HintConfig.cs` — `[Serializable]`: `Trigger`, `HintText`, `HighlightPosition`, `TriggerAfterErrors`
- `Data/Configs/GraphVisibilityConfig.cs` — `[Serializable]`: `PartialReveal`, `InitialVisibleSegments`, `RevealPerCorrectAction`
- `Data/Configs/CutsceneFrame.cs` — `[Serializable]`: `Background`, `CharacterSprite`, `Emotion`, `Text`, `Duration`, `FrameAnimation`
- `Data/Configs/AnswerOption.cs` — `[Serializable]`: идентификатор, текст/значение варианта, флаг `IsCorrect`
- `Data/Configs/ShopItem.cs` — `[Serializable]`: `ItemId`, категория, цена, описание

**Зависимости:** 1.1 (нужен asmdef Core).

**Критерий завершения:** все enum-ы и структуры компилируются, видны в инспекторе при использовании в SO.

---

### Задача 1.3 — ScriptableObject-определения данных

**Суть:** создать SO-классы для конфигурации уровней, секторов, функций и катсцен.

**Файлы:**

- `Data/ScriptableObjects/SectorData.cs` — поля: `SectorId`, `DisplayName`, `SectorIndex`, `Levels[]`, `PreviousSector`, `RequiredStarsToUnlock`, визуальные поля (спрайты, цвета, углы), `IntroCutscene`, `OutroCutscene`. Атрибут `[CreateAssetMenu(menuName = "StarFunc/Data/SectorData")]`.
- `Data/ScriptableObjects/LevelData.cs` — поля: `LevelId`, `LevelIndex`, `Type`, координатная плоскость (`PlaneMin`, `PlaneMax`, `GridStep`), `Stars[]`, `TaskType`, `ReferenceFunctions[]`, `AnswerOptions[]`, `AccuracyThreshold`, `StarRating`, ограничения (`MaxAttempts`, `MaxAdjustments`), видимость (`UseMemoryMode`, `MemoryDisplayDuration`, `GraphVisibility`), подсказки (`ShowHints`, `Hints[]`), `FragmentReward`. Атрибут `[CreateAssetMenu]`.
- `Data/ScriptableObjects/FunctionDefinition.cs` — поля: `Type (FunctionType)`, `Coefficients[]`, `DomainRange`. Атрибут `[CreateAssetMenu]`.
- `Data/ScriptableObjects/CutsceneData.cs` — поля: `CutsceneId`, `Frames[]`. Атрибут `[CreateAssetMenu]`.

**Зависимости:** 1.2.

**Критерий завершения:** можно создать SO-ассеты через меню `Assets > Create > StarFunc/Data/...`, все поля отображаются в инспекторе.

---

### Задача 1.4 — Runtime-модели данных

**Суть:** создать модели данных, используемые во время выполнения (не SO, а обычные классы/структуры).

**Файлы:**

- `Data/Runtime/PlayerSaveData.cs` — `[Serializable]`: словари прогресса секторов/уровней, `CurrentSectorIndex`, `TotalFragments`, `CurrentLives`, `LastLifeRestoreTimestamp`, статистика.
- `Data/Runtime/SectorProgress.cs` — `[Serializable]`: `State (SectorState)`, `StarsCollected`, `ControlLevelPassed`.
- `Data/Runtime/LevelProgress.cs` — `[Serializable]`: `IsCompleted`, `BestStars`, `BestTime`, `Attempts`.
- `Data/Runtime/LevelResult.cs` — результат прохождения: `Stars (int)`, `Time (float)`, `Errors (int)`, `FragmentsEarned`.
- `Data/Runtime/PlayerAnswer.cs` — ответ игрока для передачи в ValidationSystem: выбранная координата / функция / набор точек.
- `Data/Runtime/PlayerAction.cs` — действие игрока для стека undo: тип действия, предыдущее/новое состояние.
- `Data/Runtime/FunctionParams.cs` — текущие параметры функции для события `OnFunctionChanged`.
- `Data/Runtime/AnswerData.cs` — данные о выбранном ответе для события `OnAnswerSelected`.
- `Data/Runtime/StarData.cs` — данные о звезде для событий `OnStarCollected/Rejected`.
- `Data/Runtime/PopupData.cs` — данные для инициализации попапа (заголовок, текст, действия).
- `Data/Runtime/ValidationResult.cs` — результат валидации: `IsValid`, список ошибок, процент совпадения.

**Зависимости:** 1.2.

**Критерий завершения:** все классы компилируются, можно сериализовать `PlayerSaveData` в JSON и обратно.

---

### Задача 1.5 — SceneFlowManager и BootInitializer

**Суть:** реализовать управление сценами (аддитивная загрузка Level-сцены) и начальную инициализацию приложения.

**Файлы:**

- `Infrastructure/Scenes/SceneFlowManager.cs` — MonoBehaviour (DontDestroyOnLoad). Методы:
  - `LoadLevel(LevelData level)` — аддитивная загрузка Level-сцены поверх Hub (`SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive)`), скрывает Hub UI
  - `UnloadLevel()` — выгрузка Level-сцены (`SceneManager.UnloadSceneAsync`), восстанавливает Hub UI
  - `LoadScene(string sceneName)` — полная замена сцены (используется только для Boot → Hub)
  - Приватная корутина, которая: показывает LoadingOverlay (опционально) → загружает сцену → скрывает LoadingOverlay
- `UI/Overlays/LoadingOverlay.cs` — заглушка LoadingOverlay (наследник `UIScreen`), живёт на DontDestroyOnLoad-объекте. Минимальная реализация: полноэкранное затемнение + текст «Loading...». Методы: `Show()`, `Hide()`. Полноценная реализация — в Задаче 3.8.
- `Infrastructure/Boot/BootInitializer.cs` — MonoBehaviour на объекте в Boot-сцене:
  - В `Awake()`: создаёт и регистрирует `SceneFlowManager` (DontDestroyOnLoad)
  - Пока регистрирует только SceneFlowManager (остальные сервисы добавятся позже)
  - В `Start()`: вызывает `SceneFlowManager.LoadScene("Hub")`

**Зависимости:** 1.1, 0.2.

**Критерий завершения:** запуск из Boot-сцены → открывается Hub-сцена; вызов `LoadLevel()` загружает Level-сцену аддитивно, `UnloadLevel()` выгружает её.

---

### Задача 1.6 — CoordinatePlane: сетка, оси, ввод

**Суть:** реализовать координатную плоскость — основной игровой объект.

**Файлы:**

- `Gameplay/CoordinatePlane/CoordinatePlane.cs` — главный компонент. Хранит ссылки на подкомпоненты. Публичные свойства: `PlaneMin`, `PlaneMax`, `GridStep`. Методы: `WorldToPlane(Vector2)`, `PlaneToWorld(Vector2)` — преобразование координат.
- `Gameplay/CoordinatePlane/GridRenderer.cs` — отрисовка линий сетки. Использует `LineRenderer` или `GL.Begin/End`. Параметры: `PlaneMin`, `PlaneMax`, `GridStep`, цвет линий (приглушённый `BG_SECOND`).
- `Gameplay/CoordinatePlane/AxisRenderer.cs` — отрисовка осей X и Y (выделенным цветом, толще сетки). Стрелки на концах (опционально).
- `Gameplay/CoordinatePlane/CoordinateLabeler.cs` — размещение числовых TextMeshPro-меток вдоль осей с шагом `GridStep`.
- `Gameplay/CoordinatePlane/TouchInputHandler.cs` — обработка touch/mouse: tap → определение координаты на плоскости через raycasting + `WorldToPlane()`. Вызывает событие `OnPlaneClicked(Vector2)`.
- `Gameplay/CoordinatePlane/PlaneCamera.cs` — управление камерой/масштабом: pinch-to-zoom (для мобильных), scroll (для редактора). Ограничивает область видимости в пределах `PlaneMin`...`PlaneMax`.

**Зависимости:** 1.1 (ColorTokens), 1.2.

**Критерий завершения:** на Level-сцене видна координатная сетка с осями и метками; tap на плоскость возвращает правильную координату в консоль.

---

### Задача 1.7 — StarEntity: звезда с состояниями

**Суть:** реализовать сущность звезды — основной интерактивный элемент уровня.

**Файлы:**

- `Gameplay/Stars/StarEntity.cs` — главный MonoBehaviour звезды. Хранит `StarConfig`, текущий `StarState`. Методы: `Initialize(StarConfig)`, `SetState(StarState)`, `GetCoordinate()`.
- `Gameplay/Stars/StarVisuals.cs` — управление визуалом: SpriteRenderer, цвет по состоянию (`POINT_PRIMARY` — Active, `SUCCESS` — Placed, `ERROR` — Incorrect), glow-эффект (дочерний спрайт с аддитивным материалом).
- `Gameplay/Stars/StarAnimator.cs` — заглушки анимаций (на этом этапе — простые Coroutine): `PlayAppear()` (fade-in + scale), `PlayPlace()` (pulse), `PlayError()` (shake), `PlayRestore()` (заглушка).
- `Gameplay/Stars/StarInteraction.cs` — обработка tap/drag (через Collider2D + `IPointerClickHandler` или Input System). Вызывает событие при tap.
- `Gameplay/Stars/StarManager.cs` — управление всеми звёздами уровня. Методы: `SpawnStars(StarConfig[])` — инстанцирует звёзды по конфигу, `GetStar(string starId)`, `GetAllPlaced()`, `ResetAll()`.

**Зависимости:** 1.2, 1.3, 1.6.

**Критерий завершения:** звёзды появляются на координатной плоскости по позициям из `StarConfig[]`, tap на звезду регистрируется, состояние меняется визуально.

---

### Задача 1.8 — GhostEntity: персонаж с эмоциями

**Суть:** реализовать космического призрака — визуального аватара игрока.

**Файлы:**

- `Gameplay/Ghost/GhostEntity.cs` — главный MonoBehaviour. Хранит ссылки на подкомпоненты. Метод `SetEmotion(GhostEmotion)`.
- `Gameplay/Ghost/GhostVisuals.cs` — SpriteRenderer, glow-эффект, смена спрайта по эмоции (на этом этапе — один placeholder-спрайт, цвет меняется).
- `Gameplay/Ghost/GhostAnimator.cs` — управление Animator-ом (или Coroutine-заглушки для idle-анимации: плавное покачивание через `Mathf.Sin`).
- `Gameplay/Ghost/GhostEmotionController.cs` — подписывается на SO-события (`OnStarCollected → Happy`, `OnStarRejected → Sad`, `OnLevelCompleted → Excited`). Вызывает `GhostEntity.SetEmotion()`.
- `Gameplay/Ghost/GhostPositioner.cs` — позиционирование: на уровне стоит рядом с полем (фиксированная позиция или привязка к UI). Позиция хаба — заглушка.

**Зависимости:** 1.1 (события), 1.2 (GhostEmotion enum).

**Критерий завершения:** призрак отображается рядом с координатной плоскостью, меняет визуал при смене эмоции.

---

### Задача 1.9 — LevelController и AnswerSystem (ChooseCoordinate)

**Суть:** реализовать центральный контроллер уровня и первый режим ответов — выбор координаты.

**Файлы:**

- `Gameplay/Level/LevelController.cs` — MonoBehaviour. Состояния: `Initialize → ShowTask → AwaitInput → ValidateAnswer → (CalculateResult → ShowResult → Complete)`. Хранит ссылки на `CoordinatePlane`, `StarManager`, `AnswerSystem`, `ValidationSystem`. Получает `LevelData` SO при инициализации. Методы:
  - `Initialize(LevelData)` — настраивает плоскость, спавнит звёзды, настраивает систему ответов
  - `OnAnswerSubmitted(PlayerAnswer)` — вызывается при подтверждении ответа
  - `CompleteStar(StarConfig)` — помечает звезду как Placed
  - `FailAttempt()` — обработка ошибки
- `Gameplay/Level/AnswerSystem.cs` — адаптивная система ответов. На этом этапе реализовать только режим `ChooseCoordinate`:
  - Получает `AnswerOption[]` из `LevelData`
  - Создаёт варианты ответов (генерирует UI через AnswerPanel)
  - При выборе варианта вызывает `OnAnswerSelected` событие
  - Метод `GetCurrentAnswer() : PlayerAnswer`

**Зависимости:** 1.3, 1.6, 1.7.

**Критерий завершения:** уровень загружается из `LevelData` SO, показывает звёзды и варианты ответов, игрок может выбрать вариант и получить обратную связь.

---

### Задача 1.10 — ValidationSystem и LevelResultCalculator

**Суть:** реализовать проверку правильности ответов и подсчёт результата уровня.

**Файлы:**

- `Gameplay/Level/ValidationSystem.cs` — класс (не MonoBehaviour). Методы:
  - `ValidateCoordinate(Vector2 selected, Vector2 expected, float threshold) : bool`
  - `ValidateFunction(FunctionDefinition player, FunctionDefinition reference, float threshold) : bool` — заглушка (реализуется в Фазе 2)
  - `ValidateControlPoints(StarConfig[] placed, StarConfig[] reference) : ValidationResult`
  - `ValidateLevel(LevelData level, PlayerAnswer answer) : LevelResult`
- `Gameplay/Level/LevelResultCalculator.cs` — подсчёт результата:
  - `Calculate(LevelData level, int errors, float time) : LevelResult` — определяет количество звёзд по порогам из `StarRatingConfig`, вычисляет заработанные фрагменты.
- `Gameplay/Level/ActionHistory.cs` — стек действий для Undo/Reset:
  - `Push(PlayerAction)`, `Undo() : PlayerAction`, `Reset()`, `CanUndo : bool`
- `Gameplay/Level/LevelTimer.cs` — обёртка таймера уровня (запуск/пауза/стоп, `GetElapsedTime()`). На данном этапе — автономная реализация. Позже будет делегировать в `TimerService`.

**Зависимости:** 1.2, 1.3, 1.4.

**Критерий завершения:** правильный выбор координаты → звезда Placed; неправильный → Incorrect + счётчик ошибок; после выполнения всех заданий → подсчёт звёзд.

---

### Задача 1.11 — Минимальный LevelHUD

**Суть:** создать базовый HUD уровня: таймер, жизни, варианты ответов, кнопки Undo/Reset/Confirm.

**Файлы:**

- `UI/Base/UIScreen.cs` — абстрактный базовый класс: `Show()`, `Hide()`, `IsVisible`, CanvasGroup для fade.
- `UI/Base/UIPopup.cs` — абстрактный базовый класс попапа: `Show(PopupData)`, `Hide()`, затемнение фона.
- `UI/Screens/LevelHUD.cs` — наследник `UIScreen`. Содержит ссылки на:
  - `TimerDisplay` — отображение таймера
  - `LivesDisplay` — отображение жизней (на этом этапе — статическое число)
  - `AnswerPanel` — панель вариантов ответа
  - Кнопки: Pause, Confirm, Undo, Reset, Hint
  - Привязка кнопок к `LevelController` и `ActionHistory`
  - `HintButton` — кнопка подсказки (на этом этапе неактивна, функциональность реализуется в Задаче 3.4)
- `UI/Widgets/TimerDisplay.cs` — TextMeshPro, обновляется каждый кадр из `LevelTimer`.
- `UI/Widgets/LivesDisplay.cs` — TextMeshPro/иконки, отображает текущее число жизней.
- `UI/Widgets/AnswerPanel.cs` — генерирует кнопки-варианты ответа из `AnswerOption[]`. При нажатии вызывает `OnAnswerSelected`.

**Зависимости:** 1.9, 1.10.

**Критерий завершения:** на Level-сцене видны таймер, жизни, варианты ответов, кнопки. Игрок может пройти простой уровень от начала до конца.

---

## Фаза 2 — Основные системы

### Задача 2.1 — SaveService

**Суть:** реализовать систему сохранения и загрузки прогресса игрока.

**Файлы:**

- `Infrastructure/Save/ISaveService.cs` — интерфейс: `Load() : PlayerSaveData`, `Save(PlayerSaveData)`, `Delete()`, `HasSave() : bool`.
- `Infrastructure/Save/SaveService.cs` — реализация:
  - JSON-сериализация через `Newtonsoft.Json` (необходим для поддержки `Dictionary<string, SectorProgress>` в `PlayerSaveData`)
  - Путь: `Application.persistentDataPath + "/save.json"`
  - Контрольная сумма (SHA-256 хэш содержимого, хранится рядом) для базовой защиты от ручного редактирования
  - Автосохранение в `OnApplicationPause(true)` и `OnApplicationQuit()`
  - Версионирование формата: поле `SaveVersion` в `PlayerSaveData`

**Зависимости:** 1.4.

**Критерий завершения:** сохранение создаётся в persistentDataPath, переживает перезапуск приложения, некорректный файл определяется по контрольной сумме.

---

### Задача 2.2 — ProgressionService

**Суть:** реализовать систему прогрессии — отслеживание состояния секторов, уровней, звёзд.

**Файлы:**

- `Meta/Progression/IProgressionService.cs` — интерфейс (см. Architecture.md раздел 7.1): состояние секторов, уровней, звёзды, разблокировка, пропуск.
- `Meta/Progression/ProgressionService.cs` — реализация:
  - Хранит `PlayerSaveData` в памяти, читает/пишет через `ISaveService`
  - Разблокировка сектора: пройден контрольный уровень (индекс 18) предыдущего сектора + набран порог звёзд
  - Разблокировка уровня: пройден предыдущий уровень
  - Бонусные уровни (индексы 6, 11) — опциональны, не влияют на разблокировку
  - `SkipLevel()` — ставит 1 звезду, без фрагментов, списывает стоимость
  - Вызывает SO-события: `OnSectorUnlocked`, `OnSectorCompleted`

**Зависимости:** 2.1, 1.1, 1.3.

**Критерий завершения:** после прохождения уровня прогресс сохраняется, следующий уровень разблокируется, звёзды считаются корректно.

---

### Задача 2.3 — EconomyService

**Суть:** реализовать систему внутриигровой валюты — фрагментов.

**Файлы:**

- `Meta/Economy/IEconomyService.cs` — интерфейс: `GetFragments()`, `AddFragments(int)`, `SpendFragments(int) : bool`, `CanAfford(int) : bool`.
- `Meta/Economy/EconomyService.cs` — реализация:
  - Работает с полем `TotalFragments` в `PlayerSaveData`
  - `SpendFragments` возвращает `false` если баланс недостаточен
  - При изменении баланса вызывает SO-событие `OnFragmentsChanged`
  - Бонус за улучшение результата: при повторном прохождении уровня с бо́льшим количеством звёзд начисляются дополнительные фрагменты (разница)
  - Автосохранение через `ISaveService` после каждой транзакции

**Зависимости:** 2.1, 1.1.

**Критерий завершения:** фрагменты начисляются при прохождении уровня, баланс корректен после перезапуска.

---

### Задача 2.4 — LivesService

**Суть:** реализовать систему жизней с автовосстановлением по таймеру.

**Файлы:**

- `Meta/Lives/ILivesService.cs` — интерфейс: `GetCurrentLives()`, `GetMaxLives()`, `HasLives()`, `DeductLife()`, `RestoreLife()`, `RestoreAllLives()`, `GetTimeUntilNextRestore()`.
- `Meta/Lives/LivesService.cs` — реализация:
  - Максимум жизней: 5 (конфигурируемая константа)
  - Восстановление: 1 жизнь каждые 30 минут (конфигурируемо)
  - При инициализации: вычислить, сколько жизней восстановилось с момента `LastLifeRestoreTimestamp`
  - `DeductLife()` — списание 1 жизни + событие `OnLivesChanged`
  - При 0 жизней — `HasLives()` возвращает `false`, вход на уровень блокируется
  - Таймер восстановления работает в Update (или через Coroutine)

**Зависимости:** 2.1, 1.1.

**Критерий завершения:** жизни списываются при ошибке, восстанавливаются по таймеру, корректно пересчитываются после перезапуска.

---

### Задача 2.5 — TimerService и FeedbackService

**Суть:** реализовать сервис таймера уровня и объединённую систему обратной связи.

**Файлы:**

- `Meta/Timer/ITimerService.cs` — интерфейс: `StartTimer()`, `StopTimer()`, `PauseTimer()`, `ResumeTimer()`, `GetElapsedTime()`.
- `Meta/Timer/TimerService.cs` — реализация: `Time.realtimeSinceStartup`-based, поддержка паузы.
- `Meta/Feedback/IFeedbackService.cs` — интерфейс: `PlayFeedback(FeedbackType)`, `SetHapticsEnabled(bool)`.
- `Meta/Feedback/FeedbackService.cs` — реализация:
  - При вызове `PlayFeedback()` — вызывает соответствующий метод `AudioService` (если зарегистрирован) + вибрацию (`Handheld.Vibrate()`)
  - На данном этапе: аудио — заглушка (Debug.Log), вибрация — реальная
  - Настройка haptics сохраняется в `PlayerPrefs`

**Зависимости:** 1.1.

**Критерий завершения:** таймер корректно считает время с поддержкой паузы; FeedbackService вызывает вибрацию при ошибке (на устройстве).

---

### Задача 2.6 — UIService и LevelResultScreen

**Суть:** реализовать менеджер UI-экранов и экран результата уровня.

**Файлы:**

- `UI/Service/IUIService.cs` — интерфейс: `ShowScreen<T>()`, `HideScreen<T>()`, `ShowPopup<T>(PopupData)`, `HideAllPopups()`, `GetScreen<T>()`.
- `UI/Service/UIService.cs` — реализация:
  - Хранит ссылки на все экраны (через `GetComponentsInChildren` или ручная регистрация)
  - Стек экранов: показывает один экран, скрывает предыдущий
  - Попапы — поверх текущего экрана, могут быть несколько
- `UI/Screens/LevelResultScreen.cs` — наследник `UIScreen`:
  - Показывает: рейтинг звёзд (анимированное появление 1-3 звёзд), время, фрагменты, превью созвездия
  - Кнопки: «Далее» (следующий уровень), «Повторить», «В хаб»
  - Принимает `LevelResult` для заполнения данных
- `UI/Widgets/StarRatingDisplay.cs` — виджет отображения 0-3 звёзд (переиспользуется на экране результата и на карте).
- `UI/Widgets/FragmentsDisplay.cs` — счётчик фрагментов в TopBar.

**Зависимости:** 1.11, 1.10.

**Критерий завершения:** после прохождения уровня показывается экран результата с данными из `LevelResult`, кнопки работают.

---

### Задача 2.7 — HubScreen: карта галактики

**Суть:** реализовать экран хаба — визуальную карту секторов с прогрессией.

**Файлы:**

- `UI/Screens/HubScreen.cs` — наследник `UIScreen`:
  - Отображает 5 секторов + хаб, соединённых линиями
  - Каждый сектор: иконка + состояние (Locked/Available/InProgress/Completed)
  - При входе в сектор применяются его цвета (`AccentColor`, `StarColor` из `SectorData`)
  - При нажатии на Available/InProgress сектор → переход на `SectorScreen`
  - Счётчики: общие звёзды, фрагменты, жизни (TopBar)
  - Персонаж (Ghost) привязан к текущему сектору
  - Бэйджи уведомлений на секторах (заглушка, заполнится в Фазе 3)
  - Кнопки: магазин, настройки
  - Читает данные из `IProgressionService`

**Зависимости:** 2.2, 2.6, 1.8.

**Критерий завершения:** хаб показывает 5 секторов с правильными состояниями, можно перейти в сектор.

---

### Задача 2.8 — SectorScreen: карта уровней

**Суть:** реализовать экран сектора — последовательный список/путь уровней.

**Файлы:**

- `UI/Screens/SectorScreen.cs` — наследник `UIScreen`:
  - Получает `SectorData` SO
  - Отображает 20 уровней как точки на вертикальном/горизонтальном пути
  - Каждый уровень: номер + иконка состояния (locked/available/completed + звёзды)
  - Нажатие на доступный уровень → аддитивная загрузка Level-сцены с передачей `LevelData`
  - Прокрутка (ScrollRect) для навигации по уровням
  - Кнопка «Назад» → возврат на Hub

**Зависимости:** 2.7, 2.2.

**Критерий завершения:** экран сектора показывает 20 уровней с правильными состояниями, можно войти в доступный уровень.

---

### Задача 2.9 — GraphRenderer: отрисовка линейных функций

**Суть:** реализовать систему рендеринга графиков математических функций (начать с линейных).

**Файлы:**

- `Gameplay/Graph/GraphRenderer.cs` — главный MonoBehaviour. Хранит ссылки на подкомпоненты. Методы: `DrawFunction(FunctionDefinition)`, `Clear()`, `SetComparison(FunctionDefinition)`.
- `Gameplay/Graph/FunctionEvaluator.cs` — статический класс. Метод `Evaluate(FunctionDefinition, float x) : float`:
  - `Linear: y = a*x + b`
  - `Quadratic`, `Sinusoidal`, `Mixed` — заглушки (реализуются в Фазе 3)
- `Gameplay/Graph/CurveRenderer.cs` — отрисовка кривой через LineRenderer. Параметры: `FunctionDefinition`, диапазон X (`DomainRange`), количество сэмплов (50-100). Цвет: `LINE_PRIMARY`.
- `Gameplay/Graph/ControlPointsRenderer.cs` — отображение контрольных точек на графике (маркеры-кружки в позициях `StarConfig.Coordinate`).
- `Gameplay/Graph/ComparisonOverlay.cs` — отрисовка второго графика (эталонного) полупрозрачным цветом для сравнения.

**Зависимости:** 1.6, 1.3.

**Критерий завершения:** на координатной плоскости рисуется линейная функция `y = kx + b` по данным из `FunctionDefinition`, контрольные точки отображаются.

---

### Задача 2.10 — AnswerSystem: режим ChooseFunction

**Суть:** добавить второй режим ответов — выбор правильной функции из предложенных вариантов.

**Что сделать:**

- Расширить `Gameplay/Level/AnswerSystem.cs`:
  - Режим `ChooseFunction`: показывает 3-5 вариантов функций (текстовое описание ИЛИ мини-превью графика)
  - При выборе варианта — отрисовывает соответствующий график на `GraphRenderer` для визуального сравнения
  - Подтверждение → `ValidationSystem.ValidateFunction()` (на этом этапе — сравнение коэффициентов с эталоном)
- Расширить `AnswerPanel.cs` — поддержка отображения формул функций в вариантах ответа.
- Расширить `ValidationSystem.cs` — реализовать `ValidateFunction()` для линейных функций.

**Зависимости:** 2.9, 1.9.

**Критерий завершения:** уровень с `TaskType = ChooseFunction` работает: варианты отображаются, при выборе рисуется график, проверка корректна.

---

### Задача 2.11 — Создание SO-ассетов событий

**Суть:** создать экземпляры ScriptableObject-событий (`.asset`-файлы), описанных в Architecture.md §4.2.

**Что сделать:**

- Создать `.asset`-файлы в `Assets/ScriptableObjects/Events/` для всех ключевых событий:
  - `OnLevelStarted.asset` — `GameEvent<LevelData>`
  - `OnLevelCompleted.asset` — `GameEvent<LevelResult>`
  - `OnLevelFailed.asset` — `GameEvent`
  - `OnStarCollected.asset` — `GameEvent<StarData>`
  - `OnStarRejected.asset` — `GameEvent<StarData>`
  - `OnAnswerSelected.asset` — `GameEvent<AnswerData>`
  - `OnAnswerConfirmed.asset` — `GameEvent<bool>`
  - `OnFunctionChanged.asset` — `GameEvent<FunctionParams>`
  - `OnGraphUpdated.asset` — `GameEvent`
  - `OnSectorUnlocked.asset` — `GameEvent<SectorData>`
  - `OnSectorCompleted.asset` — `GameEvent<SectorData>`
  - `OnConstellationRestored.asset` — `GameEvent<SectorData>`
  - `OnLivesChanged.asset` — `GameEvent<int>`
  - `OnFragmentsChanged.asset` — `GameEvent<int>`
  - `OnActionUndone.asset` — `GameEvent`
  - `OnLevelReset.asset` — `GameEvent`
  - `OnLevelSkipped.asset` — `GameEvent<LevelData>`
  - `OnGhostEmotionChanged.asset` — `GameEvent<GhostEmotion>`

**Зависимости:** 1.1 (классы GameEvent/GameEvent<T> должны существовать).

**Критерий завершения:** все 18 SO-событий созданы в папке `ScriptableObjects/Events/`, видны в инспекторе.

---

### Задача 2.12 — Регистрация сервисов в BootInitializer

**Суть:** обновить `BootInitializer` для регистрации всех реализованных сервисов в ServiceLocator.

**Что сделать:**

- Обновить `Infrastructure/Boot/BootInitializer.cs`:
  - Создать и зарегистрировать: `SaveService`, `ProgressionService`, `EconomyService`, `LivesService`, `TimerService`, `FeedbackService`, `UIService`
  - Порядок инициализации: SaveService → ProgressionService → EconomyService → LivesService → остальные
  - Загрузить `PlayerSaveData` через `SaveService.Load()` и передать в сервисы прогрессии/экономики/жизней
  - После инициализации: `SceneFlowManager.LoadScene("Hub")`

**Зависимости:** 2.1–2.6, 2.11.

**Критерий завершения:** все сервисы доступны через `ServiceLocator.Get<IService>()` после загрузки Boot-сцены.

---

## Фаза 3 — Расширение механик

### Задача 3.1 — AnswerSystem: режим AdjustGraph (корректировка графика)

**Суть:** реализовать режим, в котором игрок корректирует параметры функции слайдерами или перетаскиванием кривой.

**Файлы:**

- `Gameplay/FunctionEditor/FunctionEditor.cs` — MonoBehaviour:
  - Создаёт UI-слайдеры для коэффициентов функции (k, b для линейной; a, h, k для квадратичной и т.д.)
  - При изменении слайдера → пересчитывает коэффициенты → вызывает SO-событие `OnFunctionChanged`
  - Поддержка drag-точек: перетаскивание контрольных точек на графике → пересчёт коэффициентов
  - `GetCurrentFunction() : FunctionDefinition` — текущее состояние функции

**Что ещё сделать:**

- Обновить `AnswerSystem.cs` — режим `AdjustGraph`: вместо выбора из вариантов → показать `FunctionEditor` + эталонный график (ComparisonOverlay)
- Обновить `GraphRenderer` — подписка на `OnFunctionChanged`, перерисовка в реальном времени
- Обновить `ValidationSystem` — сравнение графика игрока с эталоном по среднеквадратичному отклонению на контрольных точках

**Зависимости:** 2.9, 2.10.

**Критерий завершения:** игрок двигает слайдеры → график обновляется → при подтверждении сравнивается с эталоном.

---

### Задача 3.2 — GraphRenderer: параболы и синусоиды

**Суть:** расширить рендеринг графиков для квадратичных и тригонометрических функций.

**Что сделать:**

- Обновить `FunctionEvaluator.cs`:
  - `Quadratic: y = a*(x - h)² + k` — (Coefficients: [a, h, k])
  - `Sinusoidal: y = a*sin(b*x + c) + d` — (Coefficients: [a, b, c, d])
  - `Mixed` — комбинация (оценка по составным выражениям)
- Обновить `CurveRenderer.cs` — увеличить количество сэмплов для плавных кривых (80-100 для парабол, 100-150 для синусоид)
- Обновить `FunctionEditor.cs` — генерация слайдеров в зависимости от `FunctionType` (разное количество и имена коэффициентов)

**Зависимости:** 3.1.

**Критерий завершения:** параболы и синусоиды корректно рисуются, слайдеры работают для всех типов функций.

---

### Задача 3.3 — AnswerSystem: режимы BuildFunction, IdentifyError, RestoreConstellation

**Суть:** реализовать оставшиеся режимы заданий.

**Что сделать:**

- `BuildFunction` — полный ввод функции: игрок выбирает тип функции + задаёт все коэффициенты через `FunctionEditor`. Нет эталонного графика для сравнения (только контрольные точки).
- `IdentifyError` — tap на звёзды для выбора «лишней» (дистрактора). `StarInteraction` расширить: режим «выбор ошибочной звезды». Валидация через `IsDistractor` из `StarConfig`.
- `RestoreConstellation` — пошаговое размещение звёзд: игрок должен разместить звёзды в правильном порядке. Каждое правильное размещение раскрывает часть созвездия.

**Зависимости:** 3.2, 1.7.

**Критерий завершения:** все 6 типов заданий (`TaskType`) работают корректно.

---

### Задача 3.4 — HintSystem: подсказки

**Суть:** реализовать систему подсказок на уровнях.

**Файлы:**

- `Gameplay/Level/HintSystem.cs` — MonoBehaviour:
  - Получает `HintConfig[]` из `LevelData`
  - Отслеживает триггеры: `OnLevelStart`, `AfterErrors(N)`, `OnFirstInteraction`
  - При срабатывании — показывает UI-подсказку: текст + выделение позиции (`HighlightPosition`)
  - Подсказка исчезает по tap или через таймаут
  - Кнопка «Подсказка» в HUD — показывает первую доступную подсказку (за фрагменты, если так решим)

**Зависимости:** 1.9, 2.3 (если подсказки платные).

**Критерий завершения:** подсказки появляются по триггерам, текст отображается корректно.

---

### Задача 3.5 — Анимации звёзд и визуальное восстановление созвездий

**Суть:** заменить заглушки анимаций звёзд на полноценные, реализовать восстановление созвездия.

**Что сделать:**

- Обновить `StarAnimator.cs`:
  - `PlayAppear()` — fade-in (CanvasGroup/SpriteRenderer.color alpha 0→1) + scale (0.5→1.0) с easing
  - `PlayPlace()` — flash (белый спрайт поверх на 0.1 сек) + glow pulse (увеличение/уменьшение интенсивности glow)
  - `PlayError()` — shake (смещение position ±0.05 единиц, 3-4 цикла) + красный flash
  - `PlayRestore()` — золотой glow + линия к соседней звезде (LineRenderer между точками)
- Реализовать анимацию восстановления созвездия:
  - В `StarManager.cs` добавить `PlayConstellationRestore()` — последовательно анимирует все звёзды через `PlayRestore()`, рисует линии между ними
  - `LevelController` вызывает это после финального уровня (тип `Final` / `RestoreConstellation`)
- Использовать DOTween для анимаций (установить пакет DOTween через Package Manager или `.unitypackage` в `Plugins/`).

**Зависимости:** 1.7, 3.3.

**Критерий завершения:** анимации воспроизводятся плавно, восстановление созвездия визуально впечатляет.

---

### Задача 3.6 — Попапы: Pause, NoLives, SkipLevel, SectorUnlock

**Суть:** реализовать основные попапы, возникающие в ходе игры.

**Файлы:**

- `UI/Popups/PausePopup.cs` — кнопки: Продолжить, Настройки, Выход в хаб. Тормозит `TimerService`.
- `UI/Popups/NoLivesPopup.cs` — показывает таймер до следующего восстановления жизни + кнопка «Восстановить за фрагменты» + кнопка «Ждать».
- `UI/Popups/SkipLevelPopup.cs` — подтверждение пропуска: стоимость в фрагментах, предупреждение (1 звезда, без награды). Кнопки: Пропустить, Отмена.
- `UI/Popups/SectorUnlockPopup.cs` — анимированное сообщение о разблокировке нового сектора.

**Зависимости:** 2.6 (UIService), 2.3, 2.4.

**Критерий завершения:** все попапы открываются/закрываются корректно, кнопки вызывают правильные действия сервисов.

---

### Задача 3.7 — NotificationService

**Суть:** реализовать систему бэйджей и уведомлений.

**Файлы:**

- `Meta/Notifications/INotificationService.cs` — интерфейс: `HasNewContent(sectorId)`, `HasUnclaimedRewards()`, `MarkSeen(contentId)`, `GetBadgeCount(context)`.
- `Meta/Notifications/NotificationService.cs` — реализация:
  - Отслеживает: новые разблокированные секторы, восстановленные жизни, доступный контент
  - Бэйджи на UI-элементах хаба (красные точки с числом)
  - `MarkSeen()` убирает бэйдж после просмотра

**Зависимости:** 2.2, 2.4, 2.7.

**Критерий завершения:** на хабе появляются бэйджи при разблокировке сектора, бэйджи исчезают после просмотра.

---

### Задача 3.8 — LoadingOverlay и TransitionOverlay

**Суть:** реализовать UI-оверлей загрузки (на DontDestroyOnLoad) и визуальные переходы между экранами.

**Файлы:**

- `UI/Overlays/LoadingOverlay.cs` — наследник `UIScreen`. Полноэкранное затемнение/анимация загрузки. Методы: `Show()`, `Hide()`, `SetProgress(float)` (если нужен прогресс-бар). Применяется как страховочный экран при аддитивной загрузке Level-сцены (если загрузка занимает > N мс) и при первом переходе Boot → Hub.
- `UI/Overlays/TransitionOverlay.cs` — переходы: fade-in/fade-out (CanvasGroup alpha), slide. Методы: `TransitionIn(Action onComplete)`, `TransitionOut(Action onComplete)`.
- Интеграция с `SceneFlowManager` — использовать `LoadingOverlay` при аддитивной загрузке сцен, `TransitionOverlay` при переключении экранов.

**Зависимости:** 1.5, 2.6.

**Критерий завершения:** переход Hub ⇄ Level сопровождается плавным переходом, переключение между экранами — fade.

---

### Задача 3.9 — Создание SO-ассетов для уровней (контент)

**Суть:** создать ScriptableObject-ассеты для всех 100 уровней (5 секторов × 20 уровней).

**Что сделать:**

- Для каждого сектора создать `SectorData` SO-ассет в `Assets/ScriptableObjects/Sectors/`
- Для каждого уровня создать `LevelData` SO-ассет в `Assets/ScriptableObjects/Levels/Sector_N/`
- Для каждой эталонной функции создать `FunctionDefinition` SO-ассет в `Assets/ScriptableObjects/Functions/`
- Заполнить данные согласно шаблону сектора (Architecture.md раздел 12.2):
  - Уровни 1-2: Tutorial
  - Уровни 3-6: Normal
  - Уровень 7: Bonus
  - Уровни 8-11: Normal
  - Уровень 12: Bonus
  - Уровни 13-18: Normal
  - Уровень 19: Control
  - Уровень 20: Final
- Настроить для каждого уровня: `TaskType`, `Stars[]`, `AnswerOptions[]`, `ReferenceFunctions[]`, `StarRating`, `FragmentReward`
- Рекомендация: написать Editor-скрипт для массовой генерации шаблонов SO-ассетов

**Зависимости:** 3.1-3.3 (все механики должны быть готовы для заполнения).

**Критерий завершения:** все 100 уровней имеют SO-ассеты, каждый уровень проходим.

---

## Фаза 4 — Полировка и контент

### Задача 4.1 — AudioService: музыка и SFX

**Суть:** реализовать полноценную аудио-систему.

**Файлы:**

- `Meta/Audio/IAudioService.cs` — интерфейс: `PlayMusic(AudioClip)`, `StopMusic()`, `PlaySFX(AudioClip)`, `SetMusicVolume(float)`, `SetSFXVolume(float)`.
- `Meta/Audio/AudioService.cs` — реализация: хранит ссылки на `MusicPlayer` и `SFXPlayer`, делегирует вызовы.
- `Meta/Audio/MusicPlayer.cs` — фоновая музыка: два AudioSource для crossfade, метод `CrossfadeTo(AudioClip, float duration)`.
- `Meta/Audio/SFXPlayer.cs` — пул из 8-12 AudioSource. Метод `Play(AudioClip)` — берёт свободный Source, воспроизводит, возвращает в пул.
- Интеграция с `FeedbackService` — заменить Debug.Log заглушки на реальные вызовы `AudioService`.

**Зависимости:** 2.5.

**Критерий завершения:** музыка играет на хабе и уровнях с crossfade, SFX воспроизводятся при действиях игрока.

---

### Задача 4.2 — Сюжетные вставки (CutscenePopup)

**Суть:** реализовать попап для сюжетных вставок между секторами.

**Файлы:**

- `UI/Popups/CutscenePopup.cs` — наследник `UIPopup`:
  - Получает `CutsceneData` SO
  - Итерирует по `CutsceneFrame[]`: показывает фон, спрайт персонажа, текст, эмоцию
  - Переход между кадрами: по tap (если `Duration == 0`) или по таймеру (`Duration > 0`)
  - Анимация текста (typewriter-эффект)
  - Кнопка «Пропустить» — закрывает катсцену
  - По завершении — вызывает callback

**Зависимости:** 1.3, 2.6.

**Критерий завершения:** катсцена показывается при входе в сектор и при его завершении, кадры сменяются корректно.

---

### Задача 4.3 — ShopService и ShopScreen

**Суть:** реализовать магазин для покупки предметов за фрагменты.

**Файлы:**

- `Meta/Shop/IShopService.cs` — интерфейс: `GetAvailableItems()`, `PurchaseItem(string itemId) : bool`, `IsItemOwned(string itemId)`.
- `Meta/Shop/ShopService.cs` — реализация. Хранит список `ShopItem[]` (из конфига SO). `PurchaseItem` вызывает `EconomyService.SpendFragments()`.
- `UI/Screens/ShopScreen.cs` — наследник `UIScreen`. Отображение товаров по категориям (подсказки, жизни, кастомизация). Кнопка покупки + подтверждение.

**Зависимости:** 2.3, 2.6.

**Критерий завершения:** магазин отображает товары, покупка списывает фрагменты, купленные предметы отмечаются.

---

### Задача 4.4 — SettingsPopup: настройки звука и вибрации

**Суть:** реализовать попап настроек.

**Файлы:**

- `UI/Popups/SettingsPopup.cs` — наследник `UIPopup`:
  - Слайдер громкости музыки → `AudioService.SetMusicVolume()`
  - Слайдер громкости SFX → `AudioService.SetSFXVolume()`
  - Переключатель вибрации → `FeedbackService.SetHapticsEnabled()`
  - Все настройки сохраняются в `PlayerPrefs`
  - Кнопка «Закрыть»

**Зависимости:** 4.1, 3.6.

**Критерий завершения:** настройки применяются в реальном времени, сохраняются между сессиями.

---

### Задача 4.5 — Онбординг первых уровней

**Суть:** настроить обучающие уровни (Tutorial) с пошаговыми подсказками.

**Что сделать:**

- Настроить `HintConfig[]` для первых 2 уровней каждого сектора:
  - Сектор 1, уровень 1: «Нажми на звезду», «Выбери правильную координату», «Подтверди ответ»
  - Сектор 1, уровень 2: «Обрати внимание на оси», «Координата (x, y)»
  - Секторы 2-5, уровни 1-2: введение в новую механику (прямые, параболы, синусоиды, смешанные)
- Реализовать затемнение экрана с подсветкой нужного элемента (mask + highlight):
  - Полноэкранный затемняющий overlay
  - «Дырка» в overlay в позиции `HighlightPosition`
  - Текст подсказки рядом
- Интеграция с `HintSystem` — туториальные подсказки обязательны (нельзя пропустить на первом прохождении)

**Зависимости:** 3.4, 3.9.

**Критерий завершения:** новый игрок проходит первые 2 уровня с пошаговым обучением без затруднений.

---

### Задача 4.6 — VFX и Particle Systems

**Суть:** добавить визуальные эффекты для ключевых игровых моментов.

**Что сделать:**

- Particle System для сбора звезды: золотые частицы, расходящиеся от точки (burst, 0.5 сек)
- Particle System для ошибки: красные частицы, shake-подобное (burst, 0.3 сек)
- Particle System для завершения уровня: фейерверк/конфетти из звёзд (burst, 2 сек)
- Particle System для восстановления созвездия: цепная реакция glow по линиям (trail + emission по LineRenderer)
- Particle System для разблокировки сектора: свечение + раскрытие
- Создать префабы в `Assets/Prefabs/Effects/`
- Интеграция с `FeedbackService` — при `PlayFeedback()` инстанцировать соответствующий VFX

**Зависимости:** 3.5, 4.1.

**Критерий завершения:** все ключевые действия сопровождаются визуальными эффектами.

---

### Задача 4.7 — Оптимизация для мобильных устройств

**Суть:** профилирование и оптимизация производительности.

**Что сделать:**

- **Sprite Atlas**: собрать все UI-спрайты и игровые объекты в атласы (`SpriteAtlas`)
- **Object Pooling**: реализовать пулинг для `StarEntity`, VFX-частиц, элементов UI-списков (варианты ответов)
- **Canvas splitting**: разделить Canvas на статичный (фон, рамки) и динамичный (таймер, анимации)
- **LineRenderer**: ограничить сэмплы (50-100 для линейных, до 150 для синусоид)
- **GC-оптимизация**: убрать аллокации в Update (кэширование, StringBuilder, пулинг)
- **Batching**: настроить ОрдерслOrder in Layer и Sorting Layers для минимизации draw calls
- **Profiling**: прогнать Unity Profiler на целевом устройстве, убедиться: 60 FPS стабильно, < 300 MB RAM, загрузка сцены < 2 сек.

**Зависимости:** все предыдущие задачи.

**Критерий завершения:** целевые метрики (60 FPS, < 300 MB, < 2 сек. загрузка) достигнуты на среднем Android-устройстве.

---

### Задача 4.8 — AnalyticsService

**Суть:** реализовать отправку аналитических событий для отслеживания поведения игроков.

**Файлы:**

- `Infrastructure/Analytics/IAnalyticsService.cs` — интерфейс: `TrackEvent(string eventName, Dictionary<string, object> parameters)`.
- `Infrastructure/Analytics/AnalyticsService.cs` — реализация:
  - На данном этапе: логирование в консоль (или Firebase Analytics, если подключён)
  - Ключевые события:
    - `level_start` (levelId, sectorId, attempt)
    - `level_complete` (levelId, stars, time, errors)
    - `level_fail` (levelId, reason, attempt)
    - `level_skip` (levelId, cost)
    - `sector_unlock` (sectorId)
    - `purchase` (itemId, cost, currency)
    - `session_start`, `session_end` (duration)
  - Регистрация в `BootInitializer`

**Зависимости:** 1.1.

**Критерий завершения:** ключевые события логируются при прохождении уровней.

---

### Задача 4.9 — Локализация (если необходима)

**Суть:** подготовить инфраструктуру для мультиязычности.

**Что сделать:**

- Определить необходимость: если игра только на русском — можно отложить
- Если нужна: создать систему ключей локализации (`LocalizationService`)
- Хранить строки в JSON-файлах в `Assets/Resources/Localization/`
- Все тексты в UI заменить на ключи локализации

**Зависимости:** все UI-задачи.

**Критерий завершения:** если реализована — переключение языка работает корректно.

---

### Задача 4.10 — Финальное тестирование и сборка

**Суть:** полное сквозное тестирование и подготовка релизной сборки.

**Что сделать:**

- Пройти все 100 уровней от начала до конца
- Проверить: сохранения, прогрессию, экономику, жизни, таймеры
- Проверить: все 6 типов заданий, все типы функций
- Проверить: катсцены, попапы, магазин, настройки
- Проверить: edge cases (0 жизней, 0 фрагментов, максимум звёзд, повторное прохождение)
- Тестирование на целевых устройствах (минимум 3 Android-устройства разных уровней)
- Подготовить APK/AAB для Google Play
- Заполнить страницу в Google Play Console (описание, скриншоты, иконка)

**Зависимости:** все предыдущие задачи.

**Критерий завершения:** стабильная сборка, все уровни проходимы, нет критических багов.

---

## Диаграмма зависимостей задач

```
Фаза 0:   0.1 → 0.2 → 0.3

Фаза 1:   0.1 → 1.1 → 1.2 → 1.3 → 1.4
                  │            │       │
                  │      1.6 ←─┘   1.5 (+ 0.2)
                  │       │
                  ├── 1.8 │
                  │       ↓
                  │  1.7 (+ 1.3, 1.6)
                  │       │
                  │       ↓
                  └─ 1.9 (+ 1.3, 1.6, 1.7)
                          │
                     1.10 (+ 1.2, 1.3, 1.4)
                          │
                     1.11 (+ 1.9, 1.10)

Фаза 2:   1.4 → 2.1 → 2.2
                  │       │
                  ├── 2.3 (+ 2.1, 1.1)
                  ├── 2.4 (+ 2.1, 1.1)
                  ├── 2.5 (+ 2.1)
                  │       │
                  │  2.6 (+ 1.11, 1.10)
                  │   │
                  │  2.7 (+ 2.2, 1.8)
                  │   │
                  │  2.8 (+ 2.7, 2.2)
                  │
                  │  2.9 (+ 1.6, 1.3)
                  │   │
                  │  2.10 (+ 2.9, 1.9)
                  │
                  │  2.11 (+ 1.1)
                  │
                  └─ 2.12 (+ 2.1–2.6, 2.11)

Фаза 3:   2.9, 2.10 → 3.1 → 3.2 → 3.3
                       3.4 (+ 1.9, 2.3)
                       3.5 (+ 1.7, 3.3)
                       3.6 (+ 2.6, 2.3, 2.4)
                       3.7 (+ 2.2, 2.4, 2.7)
                       3.8 (+ 1.5, 2.6)
                       3.9 (+ 3.1–3.3) ← контент

Фаза 4:   4.1 (+ 2.5)
           4.2 (+ 1.3, 2.6)
           4.3 (+ 2.3, 2.6)
           4.4 (+ 4.1, 3.6)
           4.5 (+ 3.4, 3.9)
           4.6 (+ 3.5, 4.1)
           4.7 (+ все)
           4.8 (+ 1.1)
           4.9 (+ все UI)
           4.10 (+ все)
```

---

## Метрики проекта

| Метрика                   | Значение                                  |
| ------------------------- | ----------------------------------------- |
| Всего файлов C#           | ~111                                      |
| Всего задач               | 45                                        |
| SO-ассетов для контента   | ~110 (5 секторов + 100 уровней + функции) |
| Сцен                      | 3                                         |
| Сервисов в ServiceLocator | 11                                        |
