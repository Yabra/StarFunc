# Структура кодовых файлов проекта STAR FUNC

> Перечислены только C#-скрипты. Сцены, префабы, арт, аудио, материалы и ScriptableObject-ассеты не указаны.
> Все пути относительно `Assets/Scripts/`.

```txt
Assets/Scripts/
│
├── Core/                                   # Ядро: сервис-локатор, события, утилиты
│   ├── ServiceLocator.cs                   # Регистрация и получение глобальных сервисов
│   ├── GameEvent.cs                        # SO-событие без параметров
│   ├── GameEventGeneric.cs                 # SO-событие с параметром (GameEvent<T>)
│   ├── GameEventListener.cs                # MonoBehaviour-слушатель SO-события
│   ├── GameEventListenerGeneric.cs         # MonoBehaviour-слушатель SO-события с параметром
│   └── ColorTokens.cs                      # Статические цветовые константы проекта
│
├── Data/                                   # Модели данных и ScriptableObject-определения
│   ├── ScriptableObjects/                  # SO-классы (определения, не ассеты)
│   │   ├── SectorData.cs                   # Конфигурация сектора (идентификатор, уровни, условия открытия, визуал, нарратив)
│   │   ├── LevelData.cs                    # Конфигурация уровня (тип, звёзды, задание, валидация, награды)
│   │   ├── FunctionDefinition.cs           # Определение математической функции (тип, коэффициенты, диапазон)
│   │   └── CutsceneData.cs                 # Конфигурация сюжетной вставки (кадры)
│   │
│   ├── Configs/                            # Сериализуемые структуры конфигурации
│   │   ├── StarConfig.cs                   # Конфигурация звезды (координата, состояние, контрольная точка)
│   │   ├── StarRatingConfig.cs             # Условия начисления 0–3 звёзд (пороги ошибок, время)
│   │   ├── HintConfig.cs                   # Конфигурация подсказки (триггер, текст, позиция)
│   │   ├── GraphVisibilityConfig.cs        # Настройки частичной видимости графика
│   │   ├── CutsceneFrame.cs                # Один кадр сюжетной вставки (фон, персонаж, текст, эмоция)
│   │   ├── AnswerOption.cs                 # Вариант ответа для режима выбора
│   │   └── ShopItem.cs                     # Товар магазина (идентификатор, категория, цена)
│   │
│   ├── Runtime/                            # Модели данных времени выполнения
│   │   ├── PlayerSaveData.cs               # Данные сохранения игрока (прогрессия, экономика, жизни, статистика)
│   │   ├── SectorProgress.cs               # Прогресс по сектору (состояние, звёзды, контрольный уровень)
│   │   ├── LevelProgress.cs                # Прогресс по уровню (завершён, лучшее время, попытки)
│   │   ├── LevelResult.cs                  # Результат прохождения уровня (звёзды, время, ошибки)
│   │   ├── PlayerAnswer.cs                 # Ответ игрока (для передачи в ValidationSystem)
│   │   ├── PlayerAction.cs                 # Действие игрока (для стека Undo в ActionHistory)
│   │   ├── FunctionParams.cs               # Текущие параметры функции (для события OnFunctionChanged)
│   │   ├── AnswerData.cs                   # Данные о выбранном ответе (для события OnAnswerSelected)
│   │   ├── StarData.cs                     # Данные о звезде (для событий OnStarCollected/Rejected)
│   │   ├── PopupData.cs                    # Данные для инициализации попапа
│   │   └── ValidationResult.cs             # Результат валидации контрольных точек
│   │
│   └── Enums/                              # Перечисления
│       ├── LevelType.cs                    # Tutorial, Normal, Bonus, Control, Final
│       ├── TaskType.cs                     # ChooseCoordinate, ChooseFunction, AdjustGraph, BuildFunction, IdentifyError, RestoreConstellation
│       ├── StarState.cs                    # Hidden, Active, Placed, Incorrect, Restored
│       ├── SectorState.cs                  # Locked, Available, InProgress, Completed
│       ├── FunctionType.cs                 # Linear, Quadratic, Sinusoidal, Mixed
│       ├── HintTrigger.cs                  # OnLevelStart, AfterErrors, OnFirstInteraction
│       ├── GhostEmotion.cs                 # Idle, Happy, Sad, Excited, Determined
│       └── FeedbackType.cs                 # StarPlaced, StarError, LevelComplete, ConstellationRestored, ButtonTap, SectorUnlock
│
├── Gameplay/                               # Игровая логика уровня
│   ├── CoordinatePlane/                    # Координатная плоскость
│   │   ├── CoordinatePlane.cs              # Главный компонент координатной плоскости
│   │   ├── GridRenderer.cs                 # Отрисовка сетки (LineRenderer / GL / SpriteShape)
│   │   ├── AxisRenderer.cs                 # Отрисовка осей X/Y с метками
│   │   ├── CoordinateLabeler.cs            # Числовые метки на осях
│   │   ├── TouchInputHandler.cs            # Обработка touch-ввода на плоскости
│   │   └── PlaneCamera.cs                  # Масштабирование/смещение области (pinch-to-zoom)
│   │
│   ├── Stars/                              # Звёзды
│   │   ├── StarEntity.cs                   # Игровая сущность звезды (MonoBehaviour)
│   │   ├── StarVisuals.cs                  # Визуал звезды (спрайт, glow)
│   │   ├── StarAnimator.cs                 # Анимации (появление, установка, ошибка, восстановление)
│   │   ├── StarInteraction.cs              # Обработка взаимодействия (tap, drag)
│   │   └── StarManager.cs                  # Управление всеми звёздами на уровне
│   │
│   ├── Graph/                              # Рендер графика функций
│   │   ├── GraphRenderer.cs                # Главный компонент отрисовки графиков
│   │   ├── FunctionEvaluator.cs            # Вычисление значений функции по коэффициентам
│   │   ├── CurveRenderer.cs                # Отрисовка кривой (LineRenderer с сэмплами)
│   │   ├── ControlPointsRenderer.cs        # Отображение контрольных точек на графике
│   │   └── ComparisonOverlay.cs            # Наложение эталонного графика для сравнения
│   │
│   ├── Ghost/                              # Персонаж (космический призрак)
│   │   ├── GhostEntity.cs                  # Главный компонент персонажа
│   │   ├── GhostVisuals.cs                 # Спрайт, glow-эффект, анимационный контроллер
│   │   ├── GhostAnimator.cs                # Управление анимациями (idle, happy, sad, excited)
│   │   ├── GhostEmotionController.cs       # Выбор эмоции на основе игровых событий
│   │   └── GhostPositioner.cs              # Позиционирование в хабе / на уровне
│   │
│   ├── Level/                              # Контроллер уровня и подсистемы
│   │   ├── LevelController.cs              # Центральный контроллер уровня (жизненный цикл)
│   │   ├── AnswerSystem.cs                 # Система ответов (выбор варианта / построение функции)
│   │   ├── ValidationSystem.cs             # Проверка правильности решения (координаты, функции, точки)
│   │   ├── LevelTimer.cs                   # Таймер уровня (отображение + аналитика)
│   │   ├── HintSystem.cs                   # Система подсказок (туториалы, подсказки по ошибкам)
│   │   ├── ActionHistory.cs                # Стек действий для Undo/Reset
│   │   └── LevelResultCalculator.cs        # Подсчёт результата (звёзды, фрагменты)
│   │
│   └── FunctionEditor/                     # Редактор функции (для AdjustGraph / BuildFunction)
│       └── FunctionEditor.cs               # Управление слайдерами параметров и drag-точками
│
├── Meta/                                   # Мета-системы (прогрессия, экономика, жизни)
│   ├── Progression/
│   │   ├── IProgressionService.cs          # Интерфейс: состояние секторов, уровней, звёзд, пропуск
│   │   └── ProgressionService.cs           # Реализация прогрессии
│   │
│   ├── Economy/
│   │   ├── IEconomyService.cs              # Интерфейс: фрагменты — начисление, расход, баланс
│   │   └── EconomyService.cs               # Реализация экономики фрагментов
│   │
│   ├── Lives/
│   │   ├── ILivesService.cs                # Интерфейс: жизни — списание, восстановление, таймер
│   │   └── LivesService.cs                 # Реализация системы жизней
│   │
│   ├── Timer/
│   │   ├── ITimerService.cs                # Интерфейс: таймер уровня (старт, пауза, остановка)
│   │   └── TimerService.cs                 # Реализация таймера уровня
│   │
│   ├── Shop/
│   │   ├── IShopService.cs                 # Интерфейс: магазин — покупка за фрагменты
│   │   └── ShopService.cs                  # Реализация магазина
│   │
│   ├── Notifications/
│   │   ├── INotificationService.cs         # Интерфейс: бэйджи, новый контент, невостребованные награды
│   │   └── NotificationService.cs          # Реализация уведомлений
│   │
│   ├── Audio/
│   │   ├── IAudioService.cs                # Интерфейс: воспроизведение музыки и SFX
│   │   ├── AudioService.cs                 # Реализация аудио-сервиса
│   │   ├── MusicPlayer.cs                  # Фоновая музыка (crossfade между треками)
│   │   └── SFXPlayer.cs                    # Звуковые эффекты (пул AudioSource)
│   │
│   └── Feedback/
│       ├── IFeedbackService.cs             # Интерфейс: объединённая обратная связь (звук + вибрация + VFX)
│       └── FeedbackService.cs              # Реализация обратной связи
│
├── UI/                                     # Код UI-экранов и виджетов
│   ├── Base/                               # Базовые классы UI
│   │   ├── UIScreen.cs                     # Базовый класс полноэкранного экрана
│   │   └── UIPopup.cs                      # Базовый класс попапа
│   │
│   ├── Service/
│   │   ├── IUIService.cs                   # Интерфейс: управление стеком UI-экранов
│   │   └── UIService.cs                    # Реализация UI-менеджера
│   │
│   ├── Screens/                            # Полноэкранные экраны
│   │   ├── HubScreen.cs                    # Карта галактики (секторы, созвездия, прогрессия)
│   │   ├── SectorScreen.cs                 # Последовательная карта уровней сектора (линейный путь)
│   │   ├── LevelHUD.cs                     # HUD игрового уровня (таймер, жизни, ответы, кнопки)
│   │   ├── LevelResultScreen.cs            # Экран результата (звёзды, время, фрагменты, созвездие)
│   │   └── ShopScreen.cs                   # Экран магазина
│   │
│   ├── Popups/                             # Попапы (поверх экранов)
│   │   ├── PausePopup.cs                   # Пауза
│   │   ├── SettingsPopup.cs                # Настройки (звук, вибрация)
│   │   ├── NoLivesPopup.cs                 # «Нет жизней» (ожидание / покупка)
│   │   ├── SectorUnlockPopup.cs            # Разблокировка нового сектора
│   │   ├── SkipLevelPopup.cs               # Подтверждение пропуска уровня за фрагменты
│   │   └── CutscenePopup.cs                # Сюжетная вставка (кадры с текстом и персонажем)
│   │
│   ├── Overlays/                           # Постоянные overlay-элементы
│   │   ├── LoadingOverlay.cs               # Экран загрузки между сценами
│   │   └── TransitionOverlay.cs            # Визуальные переходы между экранами (fade / slide)
│   │
│   └── Widgets/                            # Переиспользуемые UI-компоненты
│       ├── TimerDisplay.cs                 # Отображение таймера (TopBar)
│       ├── LivesDisplay.cs                 # Индикатор жизней (TopBar)
│       ├── FragmentsDisplay.cs             # Счётчик фрагментов (TopBar)
│       ├── AnswerPanel.cs                  # Панель вариантов ответа (BottomBar)
│       └── StarRatingDisplay.cs            # Отображение звёзд рейтинга (экран результата, карта)
│
└── Infrastructure/                         # Сохранения, загрузка сцен, аналитика
    ├── Save/
    │   ├── ISaveService.cs                 # Интерфейс: Load, Save, Delete, HasSave
    │   └── SaveService.cs                  # Реализация: JSON в persistentDataPath, контрольная сумма, автосохранение
    │
    ├── Scenes/
    │   └── SceneFlowManager.cs             # Управление загрузкой/выгрузкой сцен (аддитивная загрузка Level), порядок инициализации
    │
    ├── Analytics/
    │   ├── IAnalyticsService.cs            # Интерфейс: отправка аналитических событий
    │   └── AnalyticsService.cs             # Реализация аналитики
    │
    └── Boot/
        └── BootInitializer.cs              # Инициализация сервисов в Boot-сцене, регистрация в ServiceLocator
```

## Сводка

| Папка             | Файлов | Назначение                                                                  |
| ----------------- | ------ | --------------------------------------------------------------------------- |
| `Core/`           | 6      | Сервис-локатор, событийная система, утилиты                                 |
| `Data/`           | 30     | SO-определения, конфиги, runtime-модели, enum                               |
| `Gameplay/`       | 29     | Координатная плоскость, звёзды, графики, призрак, контроллер уровня         |
| `Meta/`           | 18     | Прогрессия, экономика, жизни, таймер, магазин, уведомления, аудио, feedback |
| `UI/`             | 22     | Экраны, попапы, оверлеи, виджеты, UI-менеджер                               |
| `Infrastructure/` | 6      | Сохранения, сцены, аналитика, инициализация                                 |
| **Итого**         | 111    |                                                                             |
