# Todo Unity — Phase 2

> Список ручных действий в редакторе Unity для Фазы 2 из [Docs/Tasks.md](Tasks.md).
> Сюда попадает только то, что **нельзя сделать кодом**: сборка сцен, настройка префабов, wiring компонентов в инспекторе, создание ассетов (ScriptableObject-ов, материалов, спрайтов), настройка Canvas / Layout и т.п.
> Чисто-кодовые части (реализация сервисов, REST-клиентов, логики слияния и т.д.) здесь не дублируются — см. [Docs/Tasks.md](Tasks.md).
>
> **Текущее фактическое состояние проекта (к моменту написания):**
> Все 18 SO-событий уже созданы в [Assets/ScriptableObjects/Events/](../Assets/ScriptableObjects/Events/) :white_check_mark:.
> Bundled-контент (5 секторов + 100 уровней JSON + balance + shop_catalog) уже лежит в [Assets/Resources/content/](../Assets/Resources/content/) :white_check_mark:.
> `BalanceConfig.asset` **не создан** — а он уже требуется сериализованным полем в [BootInitializer.cs:11](../Assets/Scripts/Infrastructure/Boot/BootInitializer.cs#L11) :x:.
> 5 `SectorData.asset` **не созданы** — сериализованное поле [BootInitializer._sectors](../Assets/Scripts/Infrastructure/Boot/BootInitializer.cs#L12) пусто :x:.
> Сцена `Hub.unity` **полностью пустая** (нет Camera, Canvas, EventSystem, UI) :x:.
> Сцена `Level.unity` содержит только Canvas с LevelHUD — нет `UIRoot` / `UIService` / `LevelResultScreen` / `GraphRenderer` :x:.
> Префабы `HubScreen`, `SectorScreen`, `SectorNode`, `LevelNode`, `StarRatingDisplay`, `FragmentsDisplay`, `LevelResultScreen` — **не созданы**. Существующие префабы: [Ghost.prefab](../Assets/Prefabs/Gameplay/Ghost.prefab), [Star.prefab](../Assets/Prefabs/Gameplay/Star.prefab), [AnswerButton.prefab](../Assets/Prefabs/UI/AnswerButton.prefab) :x:.
> Newtonsoft.Json присутствует в `Packages/packages-lock.json` (v3.2.2, транзитивная зависимость) :white_check_mark:.
> Общая предпосылка Canvas: `Render Mode: Screen Space - Overlay`, `UI Scale Mode: Scale With Screen Size`, reference resolution 1080×1920, Match = 0.5. TMP Essentials импортированы. Все сцены подключены в Build Settings в порядке Boot → Hub → Level.

---

## Задача 2.1 — LocalSaveService

Чисто кодовая задача. В редакторе:

- Проверить после первого Play, что в `Application.persistentDataPath` появились `save.json` и `save.sha256`. В Editor это:
  - Linux: `~/.config/unity3d/<Company>/<Product>/`
  - Windows: `%APPDATA%/../LocalLow/<Company>/<Product>/`
  - macOS: `~/Library/Application Support/<Company>/<Product>/`
- Для удобства можно из меню сделать `EditorUtility.RevealInFinder(Application.persistentDataPath)` (код, не ручная работа).
- Newtonsoft.Json уже резолвится как транзитивная зависимость (`packages-lock.json` → `com.unity.nuget.newtonsoft-json 3.2.2`). Если Package Manager показывает ошибку разрешения — добавить явно: `Window → Package Manager → + → Add package by name → com.unity.nuget.newtonsoft-json@3.2.2`.

---

## Задача 2.2 — ProgressionService

Чисто кодовая задача. В редакторе:

- Убедиться, что SO-события `OnSectorUnlocked.asset` и `OnSectorCompleted.asset` уже есть в [Assets/ScriptableObjects/Events/](../Assets/ScriptableObjects/Events/) (тип — `SectorDataEvent`). :white_check_mark:
- В `Boot.unity` на объекте `BootInitializer` в инспекторе проставить:
  - `Events — Progression → On Sector Unlocked` → `OnSectorUnlocked.asset`
  - `Events — Progression → On Sector Completed` → `OnSectorCompleted.asset`

---

## Задача 2.3 — LocalEconomyService

- `OnFragmentsChanged.asset` уже существует (тип `IntGameEvent`). :white_check_mark:
- В `Boot.unity → BootInitializer`, инспектор: `Events — Economy → On Fragments Changed` → `OnFragmentsChanged.asset`.
- Никаких префабов или сцен не требуется.

> Замечание по wiring’у: [FragmentsDisplay.cs](../Assets/Scripts/UI/Widgets/FragmentsDisplay.cs) **не подписывается** на SO-событие самостоятельно — обновление происходит через вызов `SetFragments(int)` из `HubScreen`/`LevelResultScreen`. Поэтому в инспекторе `FragmentsDisplay` ссылка на SO-событие НЕ нужна.

---

## Задача 2.4 — LocalLivesService

- `OnLivesChanged.asset` уже существует (тип `IntGameEvent`). :white_check_mark:
- В `Boot.unity → BootInitializer`, инспектор: `Events — Lives → On Lives Changed` → `OnLivesChanged.asset`.

### 2.4.1 Создание `BalanceConfig.asset` **(критично)**

`BalanceConfig` уже используется `BootInitializer`, `LocalEconomyService`, `LocalLivesService`, `ProgressionService`, `ContentService` (см. [BalanceConfig.cs](../Assets/Scripts/Data/ScriptableObjects/BalanceConfig.cs)), но ассета нет.

1. В Project: `Create → StarFunc → Config → BalanceConfig` (меню из атрибута `CreateAssetMenu` на [BalanceConfig.cs:5](../Assets/Scripts/Data/ScriptableObjects/BalanceConfig.cs#L5)).
2. Сохранить как `Assets/ScriptableObjects/Config/BalanceConfig.asset` (папку `Config/` придётся создать в `Assets/ScriptableObjects/` — её сейчас нет для ассетов).
3. Значения (совпадают с дефолтами кода, см. [`balance.json`](../Assets/Resources/content/balance.json) для итоговых):
   - `Max Lives = 5`
   - `Restore Interval Seconds = 1800`
   - `Restore Cost Fragments = 20`
   - `Skip Level Cost Fragments = 100`
   - `Improvement Bonus Per Star = 5`
   - `Hint Cost Fragments = 10`
4. В `Boot.unity → BootInitializer.Config → Balance Config` — перетащить созданный ассет.

> Эти значения в рантайме перезаписываются `ContentService.ApplyBalanceToConfig()` из bundled/remote `balance.json`, но SO должен существовать, иначе `BootInitializer.Start()` упадёт по null-reference.
>
> `LivesDisplay` (аналогично `FragmentsDisplay`) не подписывается на SO-событие самостоятельно — обновляется вызовом `SetLives(int)` из `HubScreen`/`LevelHUD`. Ссылка на `OnLivesChanged` в инспекторе виджета не нужна.

---

## Задача 2.5 — TimerService и FeedbackService

- Оба класса — plain C# (не MonoBehaviour), создаются в `BootInitializer.Start()`. В редакторе ничего делать не нужно.
- `FeedbackConfig.asset` **не требуется** на текущем этапе: [FeedbackService.cs](../Assets/Scripts/Meta/Feedback/FeedbackService.cs) реализован как заглушка (`Debug.Log` для аудио + `Handheld.Vibrate()` для вибрации). Аудио-клипы и реальная конфигурация обратной связи — задача Фазы 4 (4.1 AudioService).
- Для проверки вибрации нужен билд на реальное Android-устройство; в Editor `Handheld.Vibrate()` — no-op.

---

## Задача 2.6 — UIService и LevelResultScreen

Основной объём ручной работы в Unity.

### 2.6.1 UIService в сценах `Hub.unity` и `Level.unity`

На **каждой** из двух сцен нужно собрать такую иерархию:

```text
UIRoot (GameObject)
├── Canvas              (Screen Space - Overlay, Sort Order 0)
│   + CanvasScaler      (Scale With Screen Size, 1080×1920, Match 0.5)
│   + GraphicRaycaster
│   + UIService         (ссылки на Screens/Popups-контейнеры ниже)
│   ├── Screens         (пустой RectTransform; родитель UIScreen-ов)
│   └── Popups          (пустой RectTransform; родитель UIPopup-ов)
└── EventSystem         (GameObject → UI → Event System, InputSystemUIInputModule)
```

- В инспекторе `UIService` (см. [UIService.cs:12-13](../Assets/Scripts/UI/Service/UIService.cs#L12-L13)) проставить `_screenContainer` → `Screens` и `_popupContainer` → `Popups`.
- `UIService` сам регистрирует себя в `ServiceLocator` в `Awake` — в `BootInitializer` его регистрировать не нужно.
- В `Level.unity` уже есть `Canvas` от Phase 1 (LevelHUD). Вариант: либо повесить `UIService` на этот же Canvas и создать под ним `Screens`/`Popups` и переместить туда `LevelHUD`, либо сделать отдельный `UIRoot` с бо́льшим Sort Order. Рекомендую первый — один Canvas на сцену.
- В `Hub.unity` сейчас совсем пусто — добавить Camera (см. 2.7.1), UIRoot как выше и EventSystem.

### 2.6.2 LevelResultScreen (prefab + сцена)

Создать префаб `Assets/Prefabs/UI/LevelResultScreen.prefab`:

- Корневой `RectTransform` на весь экран (`anchorMin=(0,0), anchorMax=(1,1)`), `CanvasGroup` для fade.
- Полупрозрачная подложка — `Image`, цвет `#0B1020` (BG_DARK из [ColorTokens.cs](../Assets/Scripts/Core/)) alpha ≈ 0.85.
- Заголовок `TMP_Text` (`_titleText`) — текст задаёт `LevelResultScreen.Setup()` в зависимости от `result.LevelFailed`.
- Блок рейтинга — вложенный `StarRatingDisplay` префаб (см. 2.6.3), ссылка в поле `_starRating`.
- Текст времени (TMP) — `_timeText`, иконка часов рядом (опционально).
- Вложенный `FragmentsDisplay` префаб (см. 2.6.4), ссылка в поле `_fragmentsDisplay`.
- Превью созвездия — `Image` (`_constellationPreview`), по умолчанию `SetActive(false)`, активируется `SetConstellationPreview(sprite)`.
- Три `Button` с TMP-подписями: «Далее» (`_nextButton`), «Повторить» (`_retryButton`), «В хаб» (`_hubButton`). **OnClick в инспекторе НЕ заполнять** — подписки происходят в [`LevelResultScreen.Awake()`](../Assets/Scripts/UI/Screens/LevelResultScreen.cs#L36-L46) через C#-события `OnNextClicked` / `OnRetryClicked` / `OnHubClicked`.
- На корне префаба повесить компонент `LevelResultScreen`, проставить в инспекторе все перечисленные выше ссылки.

Поместить экземпляр в `Level.unity → UIRoot/Canvas/Screens`, по умолчанию выключенный (`SetActive(false)`) — `UIService.CollectScreens()` находит через `GetComponentsInChildren<UIScreen>(true)` и скрывает через `Hide()`.

### 2.6.3 Связка LevelController ↔ LevelResultScreen **(не покрыто в Tasks.md, но необходимо)**

[LevelController](../Assets/Scripts/Gameplay/Level/LevelController.cs) поднимает SO-событие `OnLevelCompleted` (тип `LevelResultEvent`, payload `LevelResult`), но `LevelResultScreen` к нему **не подписан**. Нужен посредник, например простой MonoBehaviour `LevelResultBinder` на `Level.unity`:

- Серилизованные поля: `LevelResultEvent _onLevelCompleted`, `GameEvent _onLevelFailed`, `LevelResultScreen _screen`, ссылка на `SceneFlowManager` (или вызов через `ServiceLocator.Get<IUIService>().ShowScreen<LevelResultScreen>()`).
- В `OnEnable`: `_onLevelCompleted.AddListener(HandleCompleted)`, `_onLevelFailed.AddListener(HandleFailed)`.
- `HandleCompleted(LevelResult)` → `_screen.Setup(result); uiService.ShowScreen<LevelResultScreen>();`
- Подписаться в коде на `_screen.OnNextClicked/OnRetryClicked/OnHubClicked` и дёргать `SceneFlowManager` (загрузка следующего уровня / перезагрузка / возврат в Hub).

Если код посредника не написан — открыть задачу: **либо расширить `LevelController` подпиской напрямую на кнопки LevelResultScreen, либо добавить `LevelResultBinder` (чисто код)**. В редакторе после появления этого компонента: положить на корень `Level.unity`, проставить все 4 ссылки в инспекторе.

### 2.6.4 StarRatingDisplay (prefab)

Создать `Assets/Prefabs/UI/StarRatingDisplay.prefab`:

- `HorizontalLayoutGroup` (spacing 8, childAlignment=Middle), 3 дочерних `Image` (иконки звёзд, размер ~96×96).
- Компонент [`StarRatingDisplay`](../Assets/Scripts/UI/Widgets/StarRatingDisplay.cs) на корне, в инспекторе:
  - `_starImages` = массив из 3 `Image`-компонентов.
  - `_starFilled` / `_starEmpty` — спрайты (см. 2.6.6).
  - `_animationDelay`, `_punchScale`, `_punchDuration` — можно оставить дефолтами.
- Анимация «пульсации» реализована корутиной внутри компонента, **DOTween не нужен**.
- Использовать как вложенный префаб в `LevelResultScreen`, а также (другой инстанс) на `SectorScreen` TopBar / `SectorNode` / `LevelNode`.

### 2.6.5 FragmentsDisplay (prefab, TopBar-виджет)

Создать `Assets/Prefabs/UI/FragmentsDisplay.prefab`:

- `HorizontalLayoutGroup`: иконка фрагмента (`Image`) + `TMP_Text` для числа.
- Компонент [`FragmentsDisplay`](../Assets/Scripts/UI/Widgets/FragmentsDisplay.cs), в инспекторе:
  - `_fragmentsText` → TMP.
  - `_format` — по дефолту `◆ {0}` (можно изменить на спрайт-иконку в TMP через `<sprite name="fragment">`).
- Поместить как вложенный префаб в TopBar сцен `Hub.unity`, `Level.unity` (TopBar HUD) и в `LevelResultScreen`.
- :warning: **НЕ** прокидывать `OnFragmentsChanged` в инспектор виджета — компонент не подписан на SO-события.

### 2.6.6 Спрайты

Создать (или взять placeholder-ы из Unity Default Resources) и положить в `Assets/Art/Sprites/UI/`:

- `star_filled.png`, `star_empty.png` — ~96×96.
- `fragment.png` — ~64×64 (иконка ромба/кристалла).
- `clock.png` — ~48×48.
- `heart.png` — ~48×48 (для LivesDisplay).
- `lock.png` — ~48×48 (для SectorNode/LevelNode Locked-состояния).

Для каждого: `Texture Type = Sprite (2D and UI)`, `Pixels Per Unit = 100`, `Filter Mode = Bilinear`.

---

## Задача 2.7 — HubScreen: карта галактики

Сейчас сцена `Hub.unity` пустая — её нужно собрать с нуля.

### 2.7.1 Сцена Hub.unity

Создать иерархию:

```text
Hub.unity
├── MainCamera          (Orthographic, Size 10, Clear Flags: Solid Color)
├── HubBackground       (опционально: Sprite/ParticleSystem; пока пустой GameObject)
├── UIRoot              (см. 2.6.1 — Canvas + UIService + Screens/Popups)
│   └── Canvas
│       ├── Screens
│       │   ├── HubScreen       (instance of HubScreen.prefab, активный)
│       │   └── SectorScreen    (instance of SectorScreen.prefab, выключенный)
│       └── Popups              (пустой)
└── EventSystem
```

- Camera: background color `#0B1020` (BG_DARK), Culling Mask — либо всё, либо UI + Default.
- Сцена Hub должна быть добавлена в Build Settings под индексом 1 (Boot = 0, Level = 2).

### 2.7.2 HubScreen.prefab

Создать `Assets/Prefabs/UI/HubScreen.prefab`:

- Корень: `RectTransform` (anchor stretch), `CanvasGroup`, компонент [`HubScreen`](../Assets/Scripts/UI/Screens/HubScreen.cs).
- **TopBar** (`HorizontalLayoutGroup` сверху):
  - Счётчик общих звёзд: иконка звезды + `TMP_Text` → поле `_totalStarsText`.
  - Инстанс `FragmentsDisplay.prefab` → поле `_fragmentsDisplay`.
  - Инстанс `LivesDisplay` (пока самодельный виджет: сердце + TMP) → поле `_livesDisplay`. Если `LivesDisplay.prefab` не создан, собрать по месту: `HorizontalLayoutGroup` + иконка + TMP, компонент [`LivesDisplay`](../Assets/Scripts/UI/Widgets/LivesDisplay.cs).
  - Кнопки «Магазин», «Настройки» справа → поля `_shopButton`, `_settingsButton`. `OnClick` в инспекторе не нужен — обработчики в коде (пока заглушки).
- **Map area** (центр):
  - Пустой `Sectors` — контейнер для 5 точек-секторов (не обязателен — узлы могут лежать напрямую в Map area).
  - 5 инстансов `SectorNode.prefab` (см. 2.7.3), разложенных на карте (в инспекторе HubScreen ссылки — массив `_sectorNodes`).
  - `HubCenter` (спрайт — центр хаба, опционально).
  - `Connections` — контейнер для 4 соединительных линий (реализуются через `_connectionLine` поле в `SectorNodeWidget`, см. [SectorNodeWidget.cs:14](../Assets/Scripts/UI/Widgets/SectorNodeWidget.cs#L14)). Линия каждого узла — простой `Image` между предыдущим и текущим сектором.
  - Для каждого узла на карте — создать пустой `Transform` (`SectorAnchor_1..5`) в позиции узла. Массив этих Transform-ов → поле `_sectorAnchors` в `HubScreen` (используется для позиционирования Ghost).
- Компонент `HubScreen` (инспектор):
  - `_sectorNodes` = массив из 5 `SectorNodeWidget`-инстансов.
  - `_sectors` = массив из 5 `SectorData.asset` (см. 2.7.4) — **тот же** массив, что и у `BootInitializer._sectors`.
  - `_totalStarsText`, `_fragmentsDisplay`, `_livesDisplay` — ссылки на TopBar-виджеты.
  - `_ghostTransform` — Transform инстанса `Ghost.prefab` (см. 2.7.5).
  - `_sectorAnchors` — массив из 5 Transform-ов.
  - `_shopButton`, `_settingsButton`.
  - `_onSectorUnlocked` → `OnSectorUnlocked.asset` (SectorDataEvent).
  - `_onSectorCompleted` → `OnSectorCompleted.asset` (SectorDataEvent).

Поместить инстанс `HubScreen.prefab` в `Hub.unity → UIRoot/Canvas/Screens`, **активным** (это стартовый экран после Boot).

### 2.7.3 SectorNode.prefab

Создать `Assets/Prefabs/UI/SectorNode.prefab`:

- Корень: `Button` + `Image` (иконка сектора) → поля `_button`, `_sectorIcon`.
- Дочерний `Image` — кольцо состояния → `_stateRing`.
- Дочерний `Image` — линия-соединение с предыдущим сектором → `_connectionLine`.
- Дочерний GameObject — оверлей замка (`Image` замка) → `_lockOverlay` (вкл/выкл).
- Дочерний GameObject — бейдж уведомления (красный кружок + TMP) → `_notificationBadge`, по умолчанию выключен.
- TMP-подписи:
  - `_sectorNameText` — имя сектора.
  - `_starsText` — `★ {starsCollected}`.
- Компонент [`SectorNodeWidget`](../Assets/Scripts/UI/Widgets/SectorNodeWidget.cs) с wiring всех перечисленных полей.
- `OnClicked` C#-event подписывается в коде `HubScreen.BindSectorNodes()` — в инспекторе `OnClick` кнопки НЕ заполнять.

### 2.7.4 SectorData SO-ассеты **(ещё не созданы)**

`SectorData.cs` уже есть ([SectorData.cs](../Assets/Scripts/Data/ScriptableObjects/SectorData.cs)), но ни одного `.asset` нет.

1. Для каждого сектора 1..5: `Create → StarFunc → Data → SectorData`. Имена файлов: `Sector_1_Basic.asset`, …, `Sector_5_Black.asset`, путь `Assets/ScriptableObjects/Sectors/`.
2. Заполнить поля (реальные значения брать из [`Assets/Resources/content/sectors.json`](../Assets/Resources/content/sectors.json)):
   - `SectorId` (`sector_1` … `sector_5`)
   - `DisplayName` (на RU: «Базовый» / «Огненный» / и т.д.)
   - `SectorIndex` 1..5
   - `Levels` — массив **20** `LevelData` SO (см. 2.13.2)
   - `PreviousSector` — ссылка на предыдущий `SectorData` (null для 1-го)
   - `RequiredStarsToUnlock` (0, 30, 60, 90, 120 — сверить с `sectors.json`)
   - `SectorIcon` — спрайт иконки
   - `ConstellationSprite`, `ConstellationRestoredSprite` — заглушки допустимы
   - `AccentColor`, `StarColor` — из `sectors.json`
   - `ConstellationStarAngles` — массив углов для визуализации созвездия (заглушка в Phase 2)
   - `IntroCutscene`, `OutroCutscene` — null (реализуются в Phase 4)
3. Передать массив в `BootInitializer._sectors` (`Boot.unity → BootInitializer.Config → Sectors`) — те же 5 ассетов в порядке 1..5.
4. Передать тот же массив в `HubScreen._sectors` (в префабе или инстансе на Hub.unity).

### 2.7.5 Ghost на HubScreen

- Поместить инстанс `Ghost.prefab` (из Phase 1) как дочерний объект `HubScreen`.
- Его `Transform` назначить в `_ghostTransform` поле `HubScreen` — `HubScreen.UpdateGhostPosition()` перемещает призрака на `_sectorAnchors[i]` в зависимости от текущего сектора.

### 2.7.6 Переходы и кнопки

- Кнопки секторов: `OnClick` в инспекторе **не трогать** — подписки делаются в `HubScreen.BindSectorNodes()` через C#-event `SectorNodeWidget.OnClicked`.
- Кнопки «Магазин» / «Настройки» — заглушки в коде `HubScreen.OnShopClicked` / `OnSettingsClicked`. Если нужно визуальное подтверждение — создать пустой попап `NotImplementedPopup.prefab` (TMP «Скоро»).

---

## Задача 2.8 — SectorScreen: карта уровней

### 2.8.1 SectorScreen.prefab

Создать `Assets/Prefabs/UI/SectorScreen.prefab`:

- Корень: `RectTransform` (stretch), `CanvasGroup`, компонент [`SectorScreen`](../Assets/Scripts/UI/Screens/SectorScreen.cs).
- **Верхняя панель**:
  - Кнопка «Назад» → `_backButton`.
  - `TMP_Text` имя сектора → `_sectorNameText`.
  - `TMP_Text` прогресс звёзд «★ 12/60» → `_sectorStarsText`.
  - TopBar-счётчики (фрагменты, жизни) — инстансы соответствующих префабов **опционально**, не используются `SectorScreen` напрямую.
- **ScrollRect**:
  - `ScrollRect` (Vertical only) → `_scrollRect`.
  - `Viewport` (маска), под ним `Content` с `VerticalLayoutGroup` + `ContentSizeFitter` (Vertical Fit = Preferred Size).
  - `Content` → `_levelContainer`.
- Поле `_levelNodePrefab` — ссылка на `LevelNode.prefab` (см. 2.8.2).
- `_onLevelSelected` — SO-событие типа `LevelDataEvent`. **Не существует** в `Events/`: создать ассет `OnLevelSelected.asset` через `Create → StarFunc → Events → LevelDataEvent`. Подписывает любой компонент (например, `LevelLauncher`), который вызовет `SceneFlowManager.LoadLevel(level)`.
- Компонент `SectorScreen` на корне, wiring всех полей.

### 2.8.2 LevelNode.prefab

Создать `Assets/Prefabs/UI/LevelNode.prefab`:

- `Button` + фоновый `Image` → `_button`, `_nodeIcon`.
- `TMP_Text` номер уровня → `_levelNumberText`.
- Три `Image` для звёзд → `_starImages`, спрайты `_starFilled` / `_starEmpty` (те же, что в 2.6.6).
- GameObject `_lockOverlay` (иконка замка).
- `Image` соединительной линии → `_connectionLine`.
- Три state-спрайта → `_lockedSprite`, `_availableSprite`, `_completedSprite`.
- Поля цветов `_lockedColor`, `_availableColor`, `_completedColor` — можно оставить дефолтами.
- Компонент [`LevelNodeWidget`](../Assets/Scripts/UI/Widgets/LevelNodeWidget.cs) на корне.
- Node-ы инстанцируются `SectorScreen.BuildLevelNodes()` — вручную раскладывать не нужно.

### 2.8.3 Размещение SectorScreen

- Поместить инстанс `SectorScreen.prefab` в `Hub.unity → UIRoot/Canvas/Screens`, **выключенным** (`SetActive(false)`).
- `UIService.CollectScreens` найдёт его через рекурсивный поиск и зарегистрирует.

### 2.8.4 Запуск уровня из SectorScreen

- `SectorScreen` поднимает SO-событие `_onLevelSelected` (тип `LevelDataEvent`) — нужен компонент-слушатель, который вызовет `SceneFlowManager.LoadLevel(levelData)`. Варианты:
  1. Добавить обработчик в сам `SectorScreen` (код).
  2. Создать отдельный `LevelLauncher` MonoBehaviour на `Hub.unity` с сериализованным `LevelDataEvent _onLevelSelected` и подпиской в `OnEnable`.
- Убедиться, что сцена `Level.unity` добавлена в Build Settings под индексом 2.

---

## Задача 2.9 — GraphRenderer: отрисовка линейных функций

Реализация полностью процедурная — материалы и префабы-точки **не нужны**.

### 2.9.1 GraphRenderer GameObject на `Level.unity`

Создать в `Level.unity` под `CoordinatePlane`:

```text
CoordinatePlane
└── GraphRenderer                    [GraphRenderer]
    ├── CurveRenderer                [LineRenderer + CurveRenderer]
    ├── ControlPointsRenderer        [ControlPointsRenderer]
    └── ComparisonOverlay            [LineRenderer + CurveRenderer + ComparisonOverlay]
```

Детали:

- **GraphRenderer** — пустой GameObject, локальная позиция (0,0,0), масштаб в мировых единицах (совпадает с `CoordinatePlane`).
  - Компонент [`GraphRenderer`](../Assets/Scripts/Gameplay/Graph/GraphRenderer.cs). В инспекторе: `_curveRenderer`, `_controlPointsRenderer`, `_comparisonOverlay` — ссылки на три дочерних компонента.
- **CurveRenderer** — `GameObject` с `LineRenderer`:
  - Material: `Sprites/Default` (перетащить в слот `Materials[0]`). Дополнительный материал (`GraphLine.mat`) НЕ нужен — [CurveRenderer.ConfigureLineRenderer](../Assets/Scripts/Gameplay/Graph/CurveRenderer.cs#L56-L68) сам выставляет цвет (`LINE_PRIMARY`), ширину (0.06), sortingOrder (5), cap/corner vertices (4).
  - `Use World Space = false`.
  - `_sampleCount` = 80 (дефолт, можно оставить).
  - Компонент `CurveRenderer` на том же объекте (`[RequireComponent(typeof(LineRenderer))]` гарантирует LineRenderer).
- **ControlPointsRenderer** — `GameObject` с `Transform` и компонентом `ControlPointsRenderer`.
  - **Никакого префаба маркера не нужно**: [ControlPointsRenderer](../Assets/Scripts/Gameplay/Graph/ControlPointsRenderer.cs) создаёт маркеры-кружки процедурно через `LineRenderer` в `CreateMarker`.
  - В инспекторе поля `_markerRadius` (0.15), `_circleSegments` (24) можно оставить дефолтами.
- **ComparisonOverlay** — копия объекта `CurveRenderer` (включая `LineRenderer` + `CurveRenderer`), плюс компонент `ComparisonOverlay` (тоже `[RequireComponent(typeof(CurveRenderer))]`). В `Awake` `ComparisonOverlay.cs` сам выставляет прозрачность (alpha 0.35) и ширину 0.04. Material — `Sprites/Default`, как у основного.

### 2.9.2 Спрайт маркера — **не требуется**

Маркеры точек рисуются `LineRenderer`-ом процедурно. Спрайт `point_marker.png` создавать не нужно.

### 2.9.3 Материалы `GraphLine.mat` / `GraphLineCompare.mat` — **не требуется**

Весь цвет / ширина / прозрачность задаются в коде. Достаточно дефолтного `Sprites/Default` Material на каждом `LineRenderer`.

### 2.9.4 Тест GraphRenderer

Для проверки в Play Mode:

- На `Level.unity` у `LevelController` в `[Header("Testing")] _levelData` назначить тестовый `LevelData.asset` с `TaskType = ChooseCoordinate`, одним `FunctionDefinition` (`Type = Linear`, `Coefficients = [1, 0]`, `DomainRange = (-10, 10)`) в `ReferenceFunctions`.
- Убедиться, что `AnswerSystem._graphRenderer` ссылается на созданный `GraphRenderer` (см. 2.10).

---

## Задача 2.10 — AnswerSystem: режим ChooseFunction

Основная работа кодовая; по UI действия минимальны.

### 2.10.1 Wiring AnswerSystem → GraphRenderer **(критично для 2.10)**

В `Level.unity` на объекте, где висит `AnswerSystem` (дочерний к `LevelController`), в инспекторе заполнить поле [`_graphRenderer`](../Assets/Scripts/Gameplay/Level/AnswerSystem.cs#L21) — перетащить созданный в 2.9 `GraphRenderer`. Без этой ссылки preview графика в `SelectOption` не сработает.

### 2.10.2 AnswerButton.prefab

Отдельный префаб `AnswerButton_Function.prefab` **не требуется**: [`AnswerPanel.OnOptionsChanged`](../Assets/Scripts/UI/Widgets/AnswerPanel.cs#L52-L83) при `TaskType = ChooseFunction` сам меняет шрифт TMP (курсив, `_formulaFontSize`) и форматирует текст формулы через `FormatFunctionFormula`. Один `AnswerButton.prefab` работает в обоих режимах.

Если хочется мини-превью графика в варианте ответа — это **выходит за рамки 2.10** (можно добавить в Phase 3/4): потребуется `FunctionPreview.prefab` с `RawImage` + `RenderTexture` + отдельная камера + копия `GraphRenderer`.

### 2.10.3 Поле `AnswerOption.Function` в тестовом `LevelData`

- В `LevelData.AnswerOptions[i].Function` перетащить ссылку на `FunctionDefinition.asset`. Тестовые `FunctionDefinition.asset` создавать через `Create → StarFunc → Data → FunctionDefinition`. Класть в `Assets/ScriptableObjects/Functions/`.
- Флаг `IsCorrect` поставить у правильного варианта.

### 2.10.4 `OnAnswerSelected.asset`

- Уже создан. В инспекторе `AnswerSystem._onAnswerSelected` — назначить `OnAnswerSelected.asset` (тип `AnswerDataEvent`).

---

## Задача 2.11 — Создание SO-ассетов событий

**Все 18 ассетов уже созданы** в [Assets/ScriptableObjects/Events/](../Assets/ScriptableObjects/Events/):

| Ассет                           | Тип                  |
|---------------------------------|----------------------|
| `OnLevelStarted.asset`          | `LevelDataEvent`     |
| `OnLevelCompleted.asset`        | `LevelResultEvent`   |
| `OnLevelFailed.asset`           | `GameEvent`          |
| `OnStarCollected.asset`         | `StarDataEvent`      |
| `OnStarRejected.asset`          | `StarDataEvent`      |
| `OnAnswerSelected.asset`        | `AnswerDataEvent`    |
| `OnAnswerConfirmed.asset`       | `BoolGameEvent`      |
| `OnFunctionChanged.asset`       | `FunctionParamsEvent`|
| `OnGraphUpdated.asset`          | `GameEvent`          |
| `OnSectorUnlocked.asset`        | `SectorDataEvent`    |
| `OnSectorCompleted.asset`       | `SectorDataEvent`    |
| `OnConstellationRestored.asset` | `SectorDataEvent`    |
| `OnLivesChanged.asset`          | `IntGameEvent`       |
| `OnFragmentsChanged.asset`      | `IntGameEvent`       |
| `OnActionUndone.asset`          | `GameEvent`          |
| `OnLevelReset.asset`            | `GameEvent`          |
| `OnLevelSkipped.asset`          | `LevelDataEvent`     |
| `OnGhostEmotionChanged.asset`   | `GhostEmotionEvent`  |

Действие в редакторе — **wiring** (назначить эти ассеты в сериализованные поля всех MonoBehaviour-ов, которые их используют):

- **BootInitializer** (Boot.unity): `OnFragmentsChanged`, `OnLivesChanged`, `OnSectorUnlocked`, `OnSectorCompleted`. См. 2.12.
- **LevelController** (Level.unity, [LevelController.cs:38-43](../Assets/Scripts/Gameplay/Level/LevelController.cs#L38-L43)): `OnLevelStarted`, `OnLevelCompleted`, `OnLevelFailed`, `OnStarCollected`, `OnStarRejected`, `OnAnswerConfirmed`.
- **AnswerSystem** (Level.unity): `OnAnswerSelected`.
- **HubScreen.prefab**: `OnSectorUnlocked`, `OnSectorCompleted`.
- **SectorScreen.prefab**: `OnLevelSelected` (см. замечание в 2.8.1 — ассет нужно создать, он не в списке выше).
- **GhostEmotionController** (Level.unity, если используется): `OnLevelCompleted` (тип расхождение — см. ниже) + `OnGhostEmotionChanged`.
- **LevelResultBinder** (если реализован, см. 2.6.3): `OnLevelCompleted`, `OnLevelFailed`.

> :warning: Несоответствие типа в `GhostEmotionController._onLevelCompleted` (ожидает `GameEvent`, а `OnLevelCompleted.asset` — `LevelResultEvent`) — это bug в коде, не wiring. Либо создать отдельный `OnLevelCompletedNotification.asset` (GameEvent), либо поменять поле на generic. Вынести в отдельный код-тикет.

Если какой-то нужный ассет отсутствует (напр. `OnLevelSelected.asset`) — создать через `Create → StarFunc → Events → <нужный тип>`.

---

## Задача 2.12 — Регистрация сервисов в BootInitializer

- `Boot.unity` уже существует и содержит `BootInitializer`, `LoadingOverlay`, `EventSystem`, `MainCamera`. :white_check_mark:
- На `BootInitializer` проставить в инспекторе:
  - **Config → Balance Config** → `BalanceConfig.asset` (см. 2.4.1).
  - **Config → Sectors** → массив из 5 `SectorData.asset` (см. 2.7.4) в порядке 1..5.
  - **Events — Economy → On Fragments Changed** → `OnFragmentsChanged.asset`.
  - **Events — Lives → On Lives Changed** → `OnLivesChanged.asset`.
  - **Events — Progression → On Sector Unlocked** → `OnSectorUnlocked.asset`.
  - **Events — Progression → On Sector Completed** → `OnSectorCompleted.asset`.
- Порядок сцен в Build Settings: `Boot (0) → Hub (1) → Level (2)`.
- На каждой сцене ровно **один** `EventSystem` и **один** `AudioListener` (без этого — warnings).
  - Boot.unity: EventSystem :white_check_mark:, AudioListener на MainCamera :white_check_mark:.
  - Hub.unity: сейчас нет ни того, ни другого — добавить в 2.7.1.
  - Level.unity: EventSystem :white_check_mark: (MainCamera также есть, проверить AudioListener).
- Запустить Play из `Boot.unity` и проверить: консоль без исключений, произошёл `Load Scene "Hub"`, `UIService` зарегистрирован, сервисы доступны через `ServiceLocator`.

> Задачи 2.1a/2.3a/2.4a/2.14/2.15 расширяют `BootInitializer` в коде (регистрация `HybridSaveService`/`HybridEconomyService`/`HybridLivesService`/`LevelCheckClient`/`ReconciliationHandler`). На текущий момент код регистрирует `LocalSaveService`, `LocalEconomyService`, `LocalLivesService`, `SyncQueue`, `CloudSaveClient`, `SyncProcessor`, `ContentService`. Когда добавят hybrid-обёртки — новых инспектор-полей, скорее всего, не появится (всё идёт через существующие ссылки), но перед коммитом сверяться с кодом.

---

## Задача 2.1a — CloudSaveClient, HybridSaveService, SaveMerger

Чисто кодовая задача. В редакторе — ничего, кроме проверки, что `Packages/packages-lock.json` содержит `com.unity.nuget.newtonsoft-json`. **`ApiConfig.asset` создавать не нужно** — в коде нет ни `ApiConfig`-класса, ни ссылок на него; `ApiClient` использует константу `ApiEndpoints.BaseUrl = "https://api.starfunc.app/api/v1"` ([ApiEndpoints.cs:8](../Assets/Scripts/Infrastructure/Network/ApiEndpoints.cs#L8)). Если в будущем URL захочется вынести в SO — это отдельная задача.

---

## Задача 2.3a — ServerEconomyService, HybridEconomyService

Чисто кодовая задача. `BootInitializer` получит доп. инициализацию `HybridEconomyService`-обёртки (код). Дополнительных инспектор-полей не появляется — существующей ссылки на `OnFragmentsChanged.asset` достаточно.

---

## Задача 2.4a — ServerLivesService, HybridLivesService

Чисто кодовая задача. В инспекторе — проверить, что `BalanceConfig.asset` и `OnLivesChanged.asset` по-прежнему назначены в `BootInitializer` (уже покрыто 2.4/2.12).

---

## Задача 2.13 — ContentService

### 2.13.1 Bundled fallback контент — **уже на месте** :white_check_mark:

Папка [Assets/Resources/content/](../Assets/Resources/content/) содержит:

- `sectors.json`
- `balance.json`
- `shop_catalog.json`
- `levels/sector_1.json` … `levels/sector_5.json`

Кодировка UTF-8, `TextAsset` импорт по умолчанию. Проверить, что файлы валидны (JSON-линтер + схемы из [API.md](API.md)).

### 2.13.2 ScriptableObject соответствие

[ContentService](../Assets/Scripts/Infrastructure/Network/ContentService.cs) работает в рантайме с DTO-объектами, **не** с SO. `ContentService.ToLevelData(dto)` умеет конвертировать DTO → `LevelData` через `ScriptableObject.CreateInstance<LevelData>()`, но это in-memory инстанс — на диск не пишется.

Отдельные `LevelData.asset` на диске всё равно нужны для режимов, где `SectorData.Levels` заполняется в инспекторе:

- Вариант **А (рекомендуемый для Phase 2)**: создать 100 пустых `LevelData.asset` (по 20 на сектор) в `Assets/ScriptableObjects/Levels/Sector_N/`, имена `S{N}_L{MM}_{Type}.asset` (например `S1_L01_Normal.asset`, `S1_L06_Bonus.asset`, `S1_L18_Control.asset`). Поля заполнять не обязательно — `ContentService.ApplyBalanceToConfig()` и runtime-слои пересчитают данные из JSON при необходимости. Эти ассеты нужны, чтобы:
  - `SectorData.Levels` (typed `LevelData[]`) можно было заполнить.
  - `SectorScreen.BuildLevelNodes` ([SectorScreen.cs:65](../Assets/Scripts/UI/Screens/SectorScreen.cs#L65)) мог итерироваться по `_currentSector.Levels`.
- Вариант **Б**: модифицировать `ContentService` так, чтобы он при загрузке создавал `LevelData`-инстансы и присваивал их в `SectorData.Levels`. Тогда на диске ничего не нужно. Это код-работа, не редакторская.

**Текущее состояние**: папка `Assets/ScriptableObjects/Levels/` пуста — нужно создавать вручную (вариант А) либо заводить код-тикет (вариант Б).

### 2.13.3 ContentConfig.asset — **не требуется**

В коде нет класса `ContentConfig`, и `ContentService` сейчас не требует конфиг-SO. Он берёт `BalanceConfig` напрямую и читает JSON из `Resources/content/` + `Application.persistentDataPath/content/`. Если появится необходимость в флаге `UseBundledOnly` — завести отдельно.

### 2.13.4 Android write permission

`Player Settings → Player → Android → Configuration → Write Permission`: оставить `Internal` (значение по умолчанию). `Application.persistentDataPath` на Android доступен без доп. прав.

---

## Задача 2.14 — Reconciliation System

Чисто кодовая задача. `ReconciliationHandler` — plain C# class, получается через `ServiceLocator.Contains<ReconciliationHandler>()` в [LevelController.Initialize](../Assets/Scripts/Gameplay/Level/LevelController.cs#L104-L107). В инспекторе **ничего делать не нужно**.

Замечание по коду: сейчас `LevelCheckClient` и `ReconciliationHandler` **не регистрируются** в `BootInitializer` — это открытый код-тикет в рамках 2.14. Пока он не закрыт, reconciliation просто не выполняется (LevelController работает на локальной валидации).

---

## Задача 2.15 — SyncQueue / SyncProcessor

Код-часть уже в [BootInitializer.cs:55-64](../Assets/Scripts/Infrastructure/Boot/BootInitializer.cs#L55-L64) (`SyncQueue`, `SyncProcessor` зарегистрированы). Оба — plain C# классы. В редакторе:

- Никаких префабов не требуется.
- Опционально (для разработки): создать debug-виджет `SyncQueueDebugView.prefab` — маленький `TMP_Text` в углу экрана, показывающий `SyncQueue.Count`. Положить на Hub/Level. В релизе — отключать.
- Тестирование offline:
  - В Editor: выключить Wi-Fi/Ethernet на хост-машине.
  - На Android: Airplane Mode.

---

## Общие чек-листы перед концом Фазы 2

### Ассеты

- [ ] `Assets/ScriptableObjects/Config/BalanceConfig.asset` создан.
- [ ] 5 `Assets/ScriptableObjects/Sectors/Sector_*.asset` созданы, все поля заполнены, порядок по `SectorIndex`.
- [ ] 100 `Assets/ScriptableObjects/Levels/Sector_N/S*_*.asset` созданы (или выбран вариант Б в 2.13.2 и зарегистрирован код-тикет).
- [ ] 18 SO-событий в `Assets/ScriptableObjects/Events/` — на месте.
- [ ] Bundled JSON в `Assets/Resources/content/` — на месте (уже так).
- [ ] `OnLevelSelected.asset` (LevelDataEvent) создан, если используется `SectorScreen._onLevelSelected`.

### Префабы

- [ ] `Assets/Prefabs/UI/LevelResultScreen.prefab`
- [ ] `Assets/Prefabs/UI/StarRatingDisplay.prefab`
- [ ] `Assets/Prefabs/UI/FragmentsDisplay.prefab`
- [ ] `Assets/Prefabs/UI/LivesDisplay.prefab` (опционально — можно собирать по месту)
- [ ] `Assets/Prefabs/UI/HubScreen.prefab`
- [ ] `Assets/Prefabs/UI/SectorScreen.prefab`
- [ ] `Assets/Prefabs/UI/SectorNode.prefab`
- [ ] `Assets/Prefabs/UI/LevelNode.prefab`

### Сцены

- [ ] `Boot.unity` — `BootInitializer` wiring: BalanceConfig, 5×SectorData, 4 SO-события (`OnFragmentsChanged`, `OnLivesChanged`, `OnSectorUnlocked`, `OnSectorCompleted`).
- [ ] `Hub.unity` — Camera, UIRoot (Canvas + UIService + Screens/Popups), EventSystem, инстанс `HubScreen.prefab` (активный), инстанс `SectorScreen.prefab` (выключен).
- [ ] `Level.unity` — UIRoot/UIService добавлены, `LevelResultScreen.prefab` положен в Screens, `GraphRenderer` иерархия создана, ссылка `GraphRenderer` проставлена в инспекторе `AnswerSystem`.
- [ ] `LevelController` SO-события назначены (6 штук: `OnLevelStarted`, `OnLevelCompleted`, `OnLevelFailed`, `OnStarCollected`, `OnStarRejected`, `OnAnswerConfirmed`).
- [ ] `LevelResultBinder` (или эквивалентный код-модуль) связывает `OnLevelCompleted` → `LevelResultScreen.Setup` + `UIService.ShowScreen<LevelResultScreen>()`.

### Общие проверки

- [ ] Все 3 сцены в Build Settings в порядке Boot → Hub → Level.
- [ ] На каждой сцене ровно один `EventSystem` и один `AudioListener`.
- [ ] Play Mode из `Boot.unity` проходит: Boot → Hub → SectorScreen → Level → LevelResultScreen → Hub. Консоль чистая.

### НЕ делать (явные анти-задачи)

- :x: Не создавать `ApiConfig.asset` — класса нет, URL захардкожен в `ApiEndpoints.BaseUrl`.
- :x: Не создавать `ContentConfig.asset` — `ContentService` его не принимает.
- :x: Не создавать `FeedbackConfig.asset` — `FeedbackService` его не читает (Phase 4 тема).
- :x: Не создавать `GraphLine.mat` / `GraphLineCompare.mat` — цвет/ширина задаются кодом; достаточно `Sprites/Default` на `LineRenderer`.
- :x: Не создавать `ControlPointMarker.prefab` — маркеры процедурные.
- :x: Не создавать `AnswerButton_Function.prefab` — `AnswerPanel` переключает стиль TMP по `TaskType` на одном префабе.
- :x: Не прокидывать `OnFragmentsChanged`/`OnLivesChanged` в инспектор `FragmentsDisplay`/`LivesDisplay` — они не подписаны.
