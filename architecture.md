# Архитектура TaskTracker

Система учёта задач и рабочего времени сотрудников. Клиент-серверное приложение: SPA на Angular взаимодействует по REST API с бэкендом на ASP.NET Core, данные хранятся в PostgreSQL.

```
┌─────────────────┐   HTTP/JSON + JWT   ┌──────────────────────┐   Npgsql    ┌────────────┐
│ Angular 19 SPA  │ ◄─────────────────► │ ASP.NET Core 8 API   │ ◄─────────► │ PostgreSQL │
│ (ui/task-tracker)│                    │ (TaskTracker.Web.Api)│  EF Core 8  │            │
└─────────────────┘                     └──────────┬───────────┘             └────────────┘
                                                   │ Bot API
                                                   ▼
                                            Telegram (уведомления админам)
```

## 1. Структура репозитория

| Путь | Назначение |
|---|---|
| `TaskTracker/` | Проект `TaskTracker.Web.Api` — хост ASP.NET Core: контроллеры, фильтры авторизации, ответы API, конфигурация (`appsettings*.json`, `config/nlog.config`), Dockerfile |
| `TaskTracker.Core/` | Доменное ядро: сущности, EF Core-контексты и миграции, сервисы бизнес-логики, фоновые задачи, ML-модуль прогноза оценок, коды ошибок с ресурсами локализации |
| `TaskTracker.Utils/` | Вспомогательные расширения (строки, коллекции и т.п.) |
| `TaskTracker.Tests/` | xUnit-тесты модуля прогнозирования оценок (`IssueEstimatePrediction/`) |
| `ui/task-tracker/` | Фронтенд Angular 19 + Playwright e2e-тесты (`e2e/`) |
| `docs/` | Документация: `testing.md` (запуск тестов), пояснительные записки курсовой/ВКР (docx), SQL-скрипты |
| `TaskTracker.DevTools/`, `TemplateEngineHost/` | Служебные артефакты IDE, в solution не входят и кода не содержат |

В solution `TaskTracker.sln` входят четыре проекта: `TaskTracker.Web.Api`, `TaskTracker.Core`, `TaskTracker.Utils`, `TaskTracker.Tests`. Все на .NET 8 (`net8.0`).

## 2. Бэкенд

### 2.1. Слои и паттерны

- **Контроллеры** (`TaskTracker/Controllers`) — тонкие: валидируют запрос, вызывают сервис, оборачивают результат в `DataResponse<T>`/`BaseApiResponse` (`TaskTracker/Responses`).
- **Сервисы** (`TaskTracker.Core/src/Services`) — вся бизнес-логика. Интерфейс + реализация в `Impl/`, регистрация через `CoreInstaller.AddCore()` (Scoped). Сервисы возвращают `IDataResult<T>` (`src/DataResult`) — обёртку «данные + список ошибок», исключения наружу не выбрасываются, а логируются и превращаются в код ошибки.
- **Репозитории** (`src/Repositories`) — выделены точечно (`IIssueRepository`, `ITimeTrackingRepository`); большинство сервисов работает с `ApplicationDbContext` напрямую через `DbContext.Set<T>()`.
- **AutoMapper** (`src/Context/AutoMappingProfile.cs`) — маппинг запросов/сущностей/моделей ответа.
- **Коды ошибок** — enum'ы в `src/Enums/ErrorCodes` (по домену: `WorkspaceErrorCodes`, `IssueErrorCodes`, `AutoTimeTrackErrorCodes`, `SosErrorCodes`…), тексты — в `.resx`-ресурсах `src/Resources/ErrorCodes` (локализуемые).

### 2.2. Конвейер приложения (`Program.cs`)

1. NLog инициализируется из `config/nlog.config` (см. §9).
2. Регистрируются два DbContext (оба на одну строку подключения `ConnectionStrings:DefaultConnection`).
3. ASP.NET Core Identity (`IdentityUser`/`IdentityRole`) поверх `ApplicationIdentityDbContext`.
4. JWT-аутентификация (см. §4).
5. `AddAutoMapper` + `AddCore()` (сервисы, репозитории, фоновые задачи).
6. CORS: политика `AllowAll` в Development, иначе `AllowFront` — только origin из `Identity:TokenAudience`, с credentials.
7. `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` — легаси-режим таймстампов Npgsql (даты хранятся без строгого требования `Kind=Utc`).
8. Если `Database:ApplyMigrations = true` — на старте применяются миграции обоих контекстов (`Database.MigrateAsync()`). Флаг добавлен для запуска в Docker без ручного `dotnet ef database update`.
9. Маршрут по умолчанию MVC (`{controller=Home}/{action=Index}`) — `HomeController` и `Views/` остались от шаблона, реальный API живёт на атрибутных маршрутах `api/*`.

## 3. Модель данных

### 3.1. Базовый класс `PersistentEntity`

Все доменные сущности наследуют `src/DataAccess/BaseClasses/PersistentEntity.cs`:

| Поле | Назначение |
|---|---|
| `Id` (int) | Первичный ключ |
| `ObjectCreateDate`, `ObjectEditDate` | Даты создания/изменения (UTC). Проставляются автоматически в переопределённых `SaveChanges*` контекста по `ChangeTracker` |
| `Version` (`[Timestamp]` byte[]) | Токен оптимистичной конкуренции |
| `IsDeleted` (bool) | **Мягкое удаление** — записи физически не удаляются, все выборки фильтруют `!IsDeleted` |

### 3.2. Сущности и связи

| Сущность | Ключевые поля | Связи |
|---|---|---|
| `User` | `Email`, `NickName`, `FirstName`/`LastName`, `Country`, `UserId` (строковый id Identity-пользователя) | 1:1 с `IdentityUser` через `UserId` |
| `Workspace` | `Name`, `WorkspaceType` (Personal/Company), `Country`, `RegistrationDate`, `Address`, `INN`, `ReviewStatus` (null для личных) | `DirectorUser` — владелец |
| `WorkspaceMember` | `TeamRole`, `UserStatus` (Active/Deleted) | `User` ↔ `Workspace` (членство) |
| `WorkspaceInvite` | `Date`, `PreviousStatus`→`NewStatus`, `RequestStatus`, `IsChecked`, `IsHidden` (зарезервировано) | `User` (кого), `Inviter` (кто), `Workspace` |
| `Project` | `Name`, `Description`, `Code` (короткий код проекта, 2–4 символа), `StartDate`, `EndDate` | `Author`, `ProjectMgr`, `Workspace` |
| `Issue` | `Name`, `Description`, `Type`, `Status`, `Priority`, `Estimate` (TimeSpan?), `Index` (порядковый номер внутри проекта) | `Author`, `Assignee` (nullable), `Project`, `Parent`/`Children` (иерархия задач) |
| `IssueStatusHistory` | `OldStatus`→`NewStatus`, `ChangedAt` (UTC) | `Issue`, `ChangedByUser` |
| `TimeTracking` | `TimeSpent`, `DateBegin`, `Comment`, `AutoTrackStatus` (null = ручное списание) | `User`, `Issue` |

Конфигурации сущностей — Fluent API в `src/DataAccess/EntityConfiguration/*` (наследуют `BaseEntityConfiguration`).

### 3.3. Перечисления

- `WorkspaceType`: `Personal`, `Company`
- `WorkspaceReviewStatus`: `OnReview`, `Approved`, `Declined`
- `UserTeamRole`: `NotSet`, `Developer`, `Tester`, `Director`, `ProjectMgr`, `Owner`
- `UserWorkspaceStatus`: `Active`, `Deleted`
- `InviteStatus`: `Default` (ожидает), `UserConfirmed`, `UserDeclined`
- `IssueType`: `Task`, `Story`, `Bug`, `Epic`
- `IssueStatus`: `Backlog`, `SelectedForDevelopment`, `InProgress`, `PullRequest`, `ToDeploy`, `Test`, `Declined`, `Done`, `Deferred`
- `IssuePriority`: `Lowest`, `Low`, `Medium`, `High`, `Highest`
- `AutoTrackTimeStatus`: `Active`, `Stopped`, `Finished`

Во многих enum'ах есть значение `All = -1` — используется как фильтр «все» в запросах.

### 3.4. Контексты и миграции

Два EF Core-контекста над **одной** базой PostgreSQL:

- `ApplicationDbContext` — доменные таблицы; миграции в `TaskTracker.Core/Migrations` (`InitialMig` 10.2024 → `VersionPersistentEtnity` → `NewDatabase` 05.2025 → `UpdateEstimate` 04.2026 → `ai_16052026` 05.2026 — история статусов и поля под ML-прогноз).
- `ApplicationIdentityDbContext` — стандартные таблицы ASP.NET Identity (`AspNetUsers`, `AspNetRoles`…); миграции в `Migrations/ApplicationIdentityDb`.

## 4. Аутентификация и авторизация

- **Регистрация** (`POST api/auth/register`): создаётся `IdentityUser` (Identity хранит пароль) и доменный `User`, связанный по `User.UserId = IdentityUser.Id`.
- **Вход** (`POST api/auth/authenticate`): проверка пароля через `SignInManager`, выпуск **JWT** (`AuthenticationService`). В токен кладутся: кастомный клейм `UserId` (int-идентификатор доменного `User` — по нему работают все сервисы), роли (`ClaimTypes.Role`), клеймы Identity-пользователя, `sub`, `email`, `iat`, `auth_time`.
- Параметры токена — секция конфигурации `Identity`: `TokenIssuer`, `TokenAudience` (совпадает с origin фронтенда и используется в CORS-политике), `TokenSecret` (симметричный ключ HMAC). Валидируются issuer, audience, lifetime и подпись; `ClockSkew = 0`.
- **Уровни доступа контроллеров**: `BaseApiController` (аноним) → `ProtectedApiController` (`[Authorize]` c JWT-схемой) → отдельные экшены с `[Authorize(Roles = Permissions.Admin)]`. Роль администратора называется **`Master`** (`Constants/Permissions.cs`).
- **Проверка членства в воркспейсе** — экшен-фильтр `WorkspaceMemberFilterAttribute`/`WorkspaceMemberAuthorizationFilter` (`TaskTracker/Attributes`). Из параметров запроса извлекается id ресурса (`WorkspaceMemberResourceType`: `Workspace`/`Project`/`Issue` или `Auto` — тип определяется по имени параметра), ресурс резолвится до воркспейса, затем проверяется, что `UserId` из токена — активный член этого воркспейса. При нарушении возвращается 401/400, а админам уходит уведомление в Telegram (потенциальная попытка обхода прав).
- **Служебный `SosController`** (`api/sos/createnewrole`, `api/sos/settorole`) — анонимные эндпоинты для создания Identity-ролей и назначения их пользователям (например, выдача роли `Master`). Защищены сравнением query-параметра `securityToken` со значением `Security:AnonymousTokenRequest` из конфигурации. Это административный «чёрный ход» для первичной настройки — в проде значение токена должно быть секретным.

## 5. REST API

Все ответы — обёртка `DataResponse<T>`: `{ data, errors[], success }`. Ошибки содержат код и локализованное сообщение.

| Контроллер | Маршрут | Эндпоинты |
|---|---|---|
| `AuthenticationController` | `api/auth` | `POST register`, `POST authenticate` |
| `UserController` | `api/user` | `GET getUser` — текущий пользователь по токену |
| `WorkspaceController` | `api/workspace` | `GET getMyWorkspaces`, `POST add`, `GET getUserInvitations`, `POST createWspInvite`, `POST searchUsersForInvite`, `GET isUserWorkspaceOwner`, `GET getUserCreatedInvites`, `POST acceptInvitationRequest`, `GET getWorkspacesForCheck` *(Master)*, `POST changeWorkspaceReviewStatus` *(Master)* |
| `ProjectController` | `api/project` | `GET getWorkspaceProjects`, `POST add`, `GET getProjectMgrCandidates` |
| `IssueController` | `api/issue` | `POST getProjectIssues`, `POST create`, `POST update`, `POST predictEstimate`, `POST trackTime` — все под `WorkspaceMemberFilter` (по Project или Issue) |
| `AutoTimeTrackController` | `api/autotrack` | `GET getActiveAutoTrack`, `POST startTracking`, `POST stopTracking` |
| `SosController` | `api/sos` | `GET createnewrole`, `GET settorole` (по `securityToken`) |

## 6. Бизнес-правила

### 6.1. Рабочие пространства (Workspace)

- **Личное** (`Personal`): у пользователя может быть только одно; `ReviewStatus` всегда `null` — модерации не требует.
- **Корпоративное** (`Company`): обязательны `INN`, `Address`, `Country`, `RegistrationDate`. Запрещены дубли по `INN` и по комбинации **страна + юр. адрес + дата регистрации**. Создаётся строго со статусом `OnReview`.
- **Модерация компаний**: администратор (`Master`) видит список воркспейсов на проверке (`getWorkspacesForCheck`) — **кроме собственных** (`DirectorUserId != adminId`, т.е. своё пространство должен подтверждать другой администратор) — и переводит их из `OnReview` только в `Approved` или `Declined`. До подтверждения пространство видят только создатель (со статусом) и администраторы.
- При создании воркспейса создателю автоматически создаётся `WorkspaceMember` с ролью `Owner`.

### 6.2. Приглашения (WorkspaceInvite)

- Владелец приглашает пользователей по **email или никнейму** (поиск только по полному совпадению — `searchUsersForInvite`).
- Приглашение после отправки нельзя изменить или удалить; пользователь видит только ожидающие (`InviteStatus.Default`) и не может получить новое, пока не обработает текущее.
- Принятие/отклонение — `acceptInvitationRequest` выставляет `UserConfirmed`/`UserDeclined`.
- **Материализация членства асинхронная**: `InviteBackgroundJob` (hosted service, цикл раз в 2 секунды) выбирает подтверждённые и необработанные инвайты (`RequestStatus = UserConfirmed`, `IsChecked = false`) и создаёт `WorkspaceMember` (роль `NotSet`, статус `Active`) либо реактивирует существующего (после удаления из воркспейса ставит `Active` обратно), после чего помечает инвайт `IsChecked = true`.
- Удаление из воркспейса — тоже через `WorkspaceInvite` (`NewStatus = Deleted`); член помечается `UserStatus = Deleted`, при этом активный автотрекинг его задач останавливается.
- `RoleManagerBackgroundJob` (раз в 60 секунд) — заготовка под автоназначение ролей, логика пока не реализована.

### 6.3. Проекты и задачи

- Проект принадлежит воркспейсу; у него есть автор, ответственный менеджер (`getProjectMgrCandidates` — выбор из членов воркспейса) и код `Code` (2–4 символа) для человекочитаемых номеров задач.
- Номер задачи `Issue.Index` — монотонный счётчик внутри проекта (`IIssueRepository.GetNextIndexAsync`), т.е. задачи адресуются как `<Code>-<Index>`.
- Задачи образуют иерархию через `ParentId` (эпик → стори → таски и т.п.).
- Создание и изменение задачи разнесены на `create`/`update`. **Каждая смена статуса записывается в `IssueStatusHistory`** (кто, когда, из какого статуса в какой) — история питает канбан-аналитику и ML-прогноз (дата завершения = первая запись с `NewStatus = Done`).
- Валидация: оценка (`Estimate`) не может быть ≤ 0; тип/статус/приоритет проверяются по спискам допустимых значений (`IssueConstants`).

### 6.4. Учёт времени

Два способа списания, оба пишут в `TimeTracking`:

1. **Ручное** (`api/issue/trackTime`): пользователь указывает затраченное время, дату начала и комментарий. `AutoTrackStatus = null`.
2. **Автотрекинг** (`api/autotrack/*`): таймер «старт/стоп» на задаче.
   - Старт (`startTracking`) разрешён, только если: пользователь — **исполнитель** задачи, задача в статусе **`InProgress`**, пользователь — член воркспейса, и в этом проекте у него **нет другого активного/приостановленного трека** (один одновременный автотрек на проект).
   - Статусы записи: `Active` → `Stopped` → `Finished`. При ручном списании времени по задаче с активным автотреком тот закрывается (`Finished`).
   - Попытка стартовать трекинг не-членом воркспейса дополнительно репортится админам в Telegram.

## 7. Прогноз оценки задачи (ML-модуль)

`IssueEstimatePredictionService` (`api/issue/predictEstimate`) прогнозирует трудоёмкость новой/редактируемой задачи. Гибрид: **эвристика + ML.NET (FastTree регрессия)**, пакет `Microsoft.ML.FastTree 5.0`.

### 7.1. Данные для обучения

- Выборка — все задачи **воркспейса** (не только проекта) с ненулевым списанным временем; текущая задача исключается. Фактическая трудоёмкость = сумма `TimeTracking.TimeSpent` по задаче.
- Дата завершения задачи берётся из `IssueStatusHistory` (первый переход в `Done`), для legacy-записей — `ObjectEditDate`.
- По выборке считаются **метрики продуктивности** (`PredictionMetrics`): средняя трудоёмкость по воркспейсу/проекту/исполнителю/типу/приоритету (усечённое среднее — отбрасывается по 10% хвостов при n≥10), «свежая» средняя исполнителя за окно 90 дней, точность оценок исполнителя (факт/оценка, кламп 0.25–4), пропускная способность (закрытых задач в неделю), текущая загрузка (открытые задачи), cycle time (создание → Done) по исполнителю/проекту/воркспейсу.

### 7.2. Эвристический прогноз

Взвешенное среднее доступных базовых оценок: исполнитель 0.35, проект 0.25, тип задачи 0.2, приоритет 0.15, воркспейс 0.1, дефолт по типу 0.1 (Epic 16 ч, Story 6 ч, Bug 3 ч, прочее 2 ч). База умножается на поправки:

| Множитель | Диапазон | Смысл |
|---|---|---|
| Сложность текста | 1..1.35 | `1 + min(0.35, √(len(name+description))/70)` — длинное описание ⇒ крупнее задача |
| Иерархия | 0.92 | Подзадачи в среднем мельче |
| Нет исполнителя | 1.08 | Неопределённость |
| Точность оценок исполнителя | 0.88..1.18 | Систематически недооценивает ⇒ прогноз выше |
| Пропускная способность | 0.9..1.1 | Быстрее среднего по воркспейсу ⇒ ниже |
| Текущая загрузка | 1..1.1 | +1% за открытую задачу, максимум +10% |
| Cycle time | 0.92..1.08 | Отношение личного cycle time к проектному |

### 7.3. ML-модель

- Включается при **≥ 10 обучающих примерах** с более чем одним различным (округлённым) значением метки.
- Обучается на лету при каждом запросе (модель не персистится): пайплайн — one-hot кодирование `Type`/`Status`/`Priority`/`Project`/`Assignee` + числовые фичи (нормированные длины текста, флаги parent/assignee, все метрики продуктивности в часах) → `FastTree` (16 листьев, 120 деревьев, `minimumExampleCountPerLeaf` 1 при n<30, иначе 3, seed 4317).
- Метка — **log(секунды)**, предсказание экспоненцируется обратно (сглаживает тяжёлый правый хвост распределения трудоёмкостей).
- Итог — блендинг: `ml*w + heuristic*(1-w)`, где w = 0.6 (10–29 примеров), 0.7 (30–59), 0.8 (60+).

### 7.4. Постобработка и ответ

- Кламп результата: 60 секунд … 30 дней; округление к шагу 30 с / 1 мин / 5 мин в зависимости от величины.
- `Confidence` 0.25–0.95 — растёт с числом примеров (общих, по проекту, по исполнителю), наличием свежих метрик исполнителя и использованием ML.
- В ответе — `EstimateSeconds`, форматированная строка (`2h 30m`), `UsedMlModel`, `TrainingSamples` и массив `Factors` (человекочитаемое объяснение: на чём основан прогноз, продуктивность исполнителя, точность его оценок, cycle time, базлайн проекта, характеристики задачи) — фронтенд показывает их в модалке задачи.
- Любая ошибка ML не роняет запрос — логируется warning и используется чистая эвристика.

Тесты модуля — `TaskTracker.Tests/IssueEstimatePrediction`: xUnit, на каждый тест создаётся и удаляется **отдельная физическая БД PostgreSQL** (`PredictionTestDatabase`), строка подключения — env `TASKTRACKER_TEST_DB_CONNECTION` (по умолчанию `localhost:5434`, БД `tasktracker_prediction_tests`; пользователю нужны права CREATE/DROP DATABASE).

## 8. Уведомления в Telegram

`LogNotificatorService` шлёт сообщения администраторам через Telegram Bot API (`GET https://api.telegram.org/bot<token>/sendMessage`). Конфигурация — секция `TelegramSettings`: `TelegramBotToken`, `AdminTelegramIds` (список chat id). Используется для алертов: сбои фоновых задач, подозрительные запросы (обращение к чужому воркспейсу, автотрекинг не-членом) — т.е. это канал мониторинга, а не пользовательские нотификации.

## 9. Логирование

NLog (`NLog.Web.AspNetCore`), конфиг — `TaskTracker/config/nlog.config`, подключается явно в `Program.cs`. Файлы в `logs/`, ротация по дате:

- `<дата>.txt` — общий лог приложения (Info+),
- `<дата>.aspnetcore.txt` — `Microsoft.AspNetCore.*`,
- `<дата>.ef.txt` — `Microsoft.EntityFrameworkCore.*` (SQL-запросы в Debug).

Сервисы логируют ошибки с параметрами вызова (структурированные плейсхолдеры), критичное дополнительно уходит в Telegram (`LogAndNotifyAdminsAsync`).

## 10. Фронтенд (`ui/task-tracker`)

- **Angular 19**, standalone-компоненты, все страницы лениво загружаются через `loadComponent` (`app.routes.ts`). SSR-инфраструктура (`@angular/ssr`, `server.ts`, Express) присутствует в проекте, но продовая сборка раздаётся как статика.
- **Маршруты**: `/login`, `/register` — публичные; `/my-workspaces` (по умолчанию), `/workspace-info`, `/all-issues` — под `UserGuard` (проверка токена/роли `User`).
- **Слой API** (`shared/services`): `ApiService` — обёртка над `HttpClient`, вручную добавляет заголовок `Authorization: Bearer <jwt>` (токен хранит `TokenService`); поверх — доменные сервисы `workspace.service`, `project.service`, `issue.service`, `autoTimeTrack.service`, `user.service`, `public.service`.
- **Окружения** (`src/environments`): dev → `https://localhost:44336/`, test → `http://localhost:58575/`, prod → `apiUrl: '/'` (API за тем же origin/реверс-прокси).
- **UI-стек**: Bootstrap 5.3 + ng-bootstrap 18, bootstrap-icons, Font Awesome, ng-select, Angular CDK. Адаптивная мобильная вёрстка.
- **Канбан-доска** (`components/all-issues`): колонки по статусам задач, перетаскивание между колонками через CDK DragDrop (смена статуса), кнопки «влево/вправо» для мобильных, цветовое выделение задач, группировка `kanbanIssuesByStatus`.
- **Модальные окна** (`shared/components/modals`): создание/редактирование задачи (включая прогноз оценки с факторами), создание проекта, воркспейса, приглашения, списание времени (`track-time`), подтверждение (`confirm`).
- **Локализация**: ngx-translate, словари `public/i18n/ru.json` и `en.json`, переключение языка в шапке (`shared/components/header`).
- Автотрекинг на карточке задачи: старт/стоп таймера, индикация активного трека (`getActiveAutoTrack` при загрузке доски).

## 11. Тестирование

Подробная инструкция — [docs/testing.md](docs/testing.md). Два слоя:

1. **Playwright e2e/API** (`ui/task-tracker/e2e`): `auth.spec.ts` (регистрация/вход), `kanban-time-tracking.spec.ts` (доска + трекинг), `api-contract.spec.ts` (контракт API), `readonly-smoke.spec.ts` (безопасный смоук для прода). Управляются переменными окружения (`E2E_APP_URL`, `E2E_API_URL`, `E2E_START_APP`, `E2E_USER_EMAIL`…). **Мутационные тесты создают реальные данные** и потому запускаются только против localhost, если явно не разрешено `E2E_ALLOW_MUTATION=1`.
2. **xUnit-тесты ML-модуля** (`TaskTracker.Tests`) — см. §7.4: изолированная одноразовая БД на каждый тест.

## 12. Docker

Оба образа собираются из корня репозитория; docker-compose в репозитории нет — контейнеры и БД запускаются вручную.

- **Бэкенд** (`TaskTracker/Dockerfile`): multi-stage — `dotnet/sdk:8.0` (restore по csproj → publish Release) → `dotnet/aspnet:8.0`. Слушает **8080**, работает от непривилегированного пользователя `app`, каталог `/app/logs` создаётся заранее под NLog. Строка подключения и секреты передаются через переменные окружения/конфигурацию; для автосоздания схемы включить `Database__ApplyMigrations=true`.
- **Фронтенд** (`ui/task-tracker/Dockerfile`): `node:22-alpine` (`npm ci` + прод-сборка Angular) → `nginx:1.27-alpine`, статика из `dist/task-tracker/browser`, порт 80. Образ копирует `nginx.conf` из корня UI-проекта — **файла сейчас нет в репозитории**, перед сборкой его нужно добавить (конфиг должен отдавать SPA с fallback на `index.html` и проксировать `/api` на бэкенд, т.к. прод-окружение использует `apiUrl: '/'`).

## 13. Конфигурация (appsettings)

| Секция | Ключи | Назначение |
|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | PostgreSQL (локально по умолчанию `localhost:5434`, БД `tasktracker`) |
| `Identity` | `TokenIssuer`, `TokenAudience`, `TokenSecret` | Параметры JWT; `TokenAudience` = origin фронтенда (используется и в CORS) |
| `TelegramSettings` | `TelegramBotToken`, `AdminTelegramIds` | Бот-уведомления администраторам |
| `Security` | `AnonymousTokenRequest` | Секрет для служебных эндпоинтов `api/sos/*` |
| `Database` | `ApplyMigrations` | Применять миграции при старте (для Docker) |
| `Logging` | стандартные уровни | Плюс полный контроль через `config/nlog.config` |

Секреты (строка подключения, `TokenSecret`, токен бота) в проде должны задаваться через переменные окружения/секрет-хранилище, а не коммититься в `appsettings.json`.

## 14. Известные ограничения и незавершённый функционал

- **Отчёты** — не реализованы (планируется).
- `RoleManagerBackgroundJob` — пустая заготовка: роли членам воркспейса автоматически не назначаются (`TeamRole = NotSet` после принятия инвайта).
- `WorkspaceInvite.IsHidden` — поле зарезервировано, скрытие обработанных запросов на фронте не реализовано.
- `ui/task-tracker/nginx.conf` отсутствует — сборка UI-образа Docker без него упадёт (см. §12).
- docker-compose отсутствует; оркестрация (API + UI + PostgreSQL) — вручную.
- `HomeController` + `Views/` — остатки MVC-шаблона, в работе SPA не участвуют.
