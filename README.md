# TaskTracker — система учёта задач и рабочего времени

Веб-приложение для управления задачами и учёта рабочего времени сотрудников IT-компаний: рабочие пространства (личные и корпоративные), проекты, канбан-доска задач, ручной и автоматический трекинг времени, а также прогноз трудоёмкости задач на основе машинного обучения.

## Возможности

- **Рабочие пространства**
  - Личные (одно на пользователя) и корпоративные (с ИНН и реквизитами компании).
  - Корпоративные пространства проходят модерацию администратором системы (статусы «на проверке / подтверждено / отклонено»).
  - Приглашение сотрудников по email или никнейму, принятие/отклонение приглашений, удаление из пространства.
- **Проекты и задачи**
  - Проекты с кодом, менеджером и сроками; задачи с типами (Task/Story/Bug/Epic), статусами, приоритетами, иерархией (родительские/дочерние) и номерами внутри проекта.
  - **Канбан-доска**: колонки по статусам, перетаскивание задач мышью, цветовое выделение, мобильная вёрстка.
  - История смен статусов каждой задачи.
- **Учёт рабочего времени**
  - Ручное списание часов с комментарием.
  - Автотрекинг: таймер «старт/стоп» на задаче (один активный таймер на проект; только исполнитель задачи в статусе «В работе»).
- **Прогноз оценки задачи (ИИ)**
  - Гибрид эвристики и ML.NET (FastTree-регрессия): прогноз строится по истории задач воркспейса, продуктивности исполнителя, точности его прошлых оценок, cycle time и другим метрикам.
  - В интерфейсе показываются уверенность прогноза и объясняющие факторы.
- **Интерфейс**
  - Локализация: русский и английский.
  - Адаптивная вёрстка для мобильных устройств.
- **Безопасность и эксплуатация**
  - Регистрация и вход по JWT (ASP.NET Core Identity), роли (пользователь / администратор системы).
  - Проверка членства в воркспейсе на каждом обращении к его ресурсам.
  - Уведомления администраторам в Telegram о сбоях и подозрительных запросах; файловое логирование (NLog).

## Технологии

| Слой | Стек |
|---|---|
| Бэкенд | C# / ASP.NET Core 8, Entity Framework Core 8, PostgreSQL 16+, ASP.NET Identity + JWT, AutoMapper, NLog, ML.NET (FastTree) |
| Фронтенд | Angular 19 (standalone-компоненты), Bootstrap 5.3 + ng-bootstrap, Angular CDK (drag-and-drop), ngx-translate |
| Тесты | Playwright (e2e/API), Karma + Jasmine (юнит-тесты UI), xUnit (тесты ML-модуля) |
| Инфраструктура | Docker + Docker Compose (PostgreSQL, API, UI) |

## Структура репозитория

```
TaskTracker/          — ASP.NET Core Web API (хост, контроллеры, конфигурация, Dockerfile)
TaskTracker.Core/     — ядро бизнес-логики: сущности, EF-контексты, миграции, сервисы, ML-модуль
TaskTracker.Utils/    — вспомогательные расширения
TaskTracker.Tests/    — xUnit-тесты прогнозирования оценок
ui/task-tracker/      — фронтенд Angular: src/ (приложение), e2e/ (Playwright), nginx.conf, Dockerfile
docker-compose.yml    — запуск всей системы (PostgreSQL + API + UI)
.env.example          — шаблон переменных окружения для docker compose
architecture.md       — техническое описание архитектуры
```

Подробное техническое описание (модель данных, API, бизнес-правила, устройство ML-прогноза) — в [architecture.md](architecture.md).

## Установка и запуск

### Требования

- .NET SDK 8.0
- Node.js 20+ (в Docker-сборке используется Node 22) и Angular CLI
- PostgreSQL 16+ (по умолчанию конфигурация ожидает `localhost:5434`, БД `tasktracker`)
- Docker с Docker Compose — если поднимаете систему целиком в контейнерах (см. [Docker](#docker)); для локального запуска из исходников не нужен

### Бэкенд

1. Создайте локальные файлы конфигурации из шаблона. Файлы `appsettings.json` и `appsettings.Development.json` содержат секреты (пароль БД, ключ подписи JWT, токен Telegram-бота), поэтому они добавлены в `.gitignore` и в репозитории их нет — в git хранится только шаблон `appsettings.example.json`. После клонирования репозитория его нужно скопировать под обоими именами и подставить свои значения:
   ```powershell
   cp TaskTracker/appsettings.example.json TaskTracker/appsettings.json
   cp TaskTracker/appsettings.example.json TaskTracker/appsettings.Development.json
   ```
   `appsettings.json` — общие настройки (используются в том числе в проде), `appsettings.Development.json` — переопределения для локальной разработки (запуск через `dotnet run` идёт с `ASPNETCORE_ENVIRONMENT=Development`, поэтому значения из него перекрывают `appsettings.json`).
2. Заполните в обоих файлах строку подключения `ConnectionStrings:DefaultConnection` и секции `Identity` (issuer/audience/секрет JWT), `TelegramSettings` и `Security` (см. [architecture.md, §13](architecture.md)).

   > **Ключ подписи JWT (`Identity:TokenSecret`) должен быть длинным — не меньше 32 символов** (как значение-заглушка в `appsettings.example.json`). Токены подписываются алгоритмом HMAC-SHA256, которому нужен ключ от 256 бит; с более коротким секретом приложение стартует, но вход в систему падает с ошибкой `IDX10653`. Используйте длинную случайную строку, например:
   > ```powershell
   > [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
   > ```
3. Примените миграции — либо автоматически при старте (`"Database": { "ApplyMigrations": true }`, включено в шаблоне), либо вручную:
   ```powershell
   dotnet ef database update --project TaskTracker.Core --startup-project TaskTracker --context ApplicationIdentityDbContext
   dotnet ef database update --project TaskTracker.Core --startup-project TaskTracker --context ApplicationDbContext
   ```
4. Один раз выпустите и доверьте сертификат для локального HTTPS (без него запуск по https падает с `Unable to configure HTTPS endpoint`):
   ```powershell
   dotnet dev-certs https --trust
   ```
5. Запустите API:
   ```powershell
   dotnet run --project TaskTracker --urls https://localhost:44336
   ```
   Порт указан не случайно: в dev-режиме фронтенд обращается к API по адресу `https://localhost:44336` (`ui/task-tracker/src/environments/environment.ts`). При запуске из Visual Studio через IIS Express этот адрес поднимается сам (`Properties/launchSettings.json`, `sslPort`), а вот профили `http`/`https`, которые использует `dotnet run` по умолчанию, слушают другие порты (5159 и 7077) — тогда фронтенд до API не достучится. Либо передавайте `--urls`, как выше, либо поправьте `apiUrl` в `environment.ts` под свой порт.

   При старте приложение само создаёт системные роли Identity — `User` (выдаётся каждому при регистрации) и `Master` (администратор системы, модерация корпоративных воркспейсов). Создание идемпотентно: если роли уже есть в БД, ничего не меняется. Заводить роль вручную запросом `api/sos/createnewrole` больше не нужно; эндпоинт остался только для нестандартных ролей.

### Фронтенд

```powershell
cd ui/task-tracker
npm install
npm start        # ng serve → http://localhost:4200
```

### Docker

В репозитории есть `docker-compose.yml`, поднимающий все три компонента (PostgreSQL, API, UI):

```powershell
cp .env.example .env    # заполните секреты (JWT-секрет, пароль БД, токен Telegram-бота и т.д.)
docker compose up -d --build
```

Здесь `appsettings.json` не нужен — вся конфигурация приходит в контейнер API переменными окружения из `.env`. Требование к длине `IDENTITY_TOKEN_SECRET` то же самое: **не меньше 32 символов**.

По умолчанию UI будет доступен на `http://localhost:4200`, API — на `http://localhost:8080`, PostgreSQL — на `localhost:5434`. Порты и секреты настраиваются через `.env` (см. `.env.example`); при первом старте API сам применит миграции (`Database__ApplyMigrations=true`).

Собрать и запустить образы по отдельности:

```powershell
# API (сборка из корня репозитория; контейнер слушает порт 8080)
docker build -t tasktracker-api -f TaskTracker/Dockerfile .

# UI (nginx, порт 80)
cd ui/task-tracker
docker build -t tasktracker-ui .
```

Строка подключения и секреты передаются контейнеру API через переменные окружения (например, `ConnectionStrings__DefaultConnection`, `Database__ApplyMigrations=true`). Образ UI раздаёт SPA с fallback на `index.html` и проксирует `/api` на контейнер API (`ui/task-tracker/nginx.conf`), так как продовая сборка использует относительный `apiUrl: '/'`.

## Тестирование

### e2e и API-тесты (Playwright)

Запускаются против уже поднятых фронтенда и API:

```powershell
cd ui/task-tracker
npx playwright install chromium   # один раз
npm run e2e
```

Наборы в `ui/task-tracker/e2e/`:

| Спека | Что проверяет |
|---|---|
| `auth.spec.ts` | регистрация и вход через интерфейс |
| `kanban-time-tracking.spec.ts` | канбан-доска, ручное списание часов, автотрекинг |
| `api-contract.spec.ts` | контракты REST API: auth, задачи проекта, трекинг, прогноз оценки |
| `readonly-smoke.spec.ts` | безопасный смоук: логин и открытие доски, без создания данных |

Мутационные тесты (первые три) создают реальных пользователей и данные, поэтому по умолчанию выполняются, только когда и API, и UI указывают на localhost. Для одноразового тестового стенда это можно снять через `E2E_ALLOW_MUTATION=1`. Против прода предназначен только `readonly-smoke.spec.ts`.

Переменные окружения:

| Переменная | По умолчанию | Назначение |
|---|---|---|
| `E2E_APP_URL` | `http://localhost:4200` | адрес фронтенда |
| `E2E_API_URL` | `https://localhost:44336` | адрес API |
| `E2E_START_APP` | — | `1` — Playwright сам поднимет `ng serve` перед прогоном |
| `E2E_ALLOW_MUTATION` | — | `1` — разрешить мутационные тесты вне localhost |
| `E2E_USER_EMAIL`, `E2E_USER_PASSWORD`, `E2E_WORKSPACE_ID`, `E2E_PROJECT_ID` | — | учётка и доска для `readonly-smoke.spec.ts` (без них он пропускается) |

Полезно: `npm run e2e:headed` — прогон с видимым браузером, `npm run e2e:ui` — интерактивный режим, `npm run e2e:report` — открыть HTML-отчёт последнего прогона.

### Юнит-тесты фронтенда (Karma + Jasmine)

```powershell
cd ui/task-tracker
npm test
```

### Тесты ML-модуля (xUnit)

```powershell
dotnet test TaskTracker.Tests/TaskTracker.Tests.csproj
```

Каждый тест создаёт собственную одноразовую БД PostgreSQL и удаляет её после прогона, поэтому нужен запущенный PostgreSQL с правом создавать/удалять базы. По умолчанию используется `localhost:5434` (пользователь `postgres`); другой сервер задаётся переменной `TASKTRACKER_TEST_DB_CONNECTION` со строкой подключения.

## Краткое руководство пользователя

- **Регистрация и вход** — email + пароль; после входа открывается раздел «Мои рабочие пространства».
- **Рабочие пространства** — создайте личное или корпоративное (корпоративное станет доступно участникам после подтверждения администратором); приглашайте сотрудников по email/никнейму.
- **Проекты** — внутри пространства создайте проект, укажите код, менеджера и сроки.
- **Задачи** — создавайте задачи с типом, приоритетом и исполнителем; перемещайте их по канбан-доске (перетаскиванием или кнопками на мобильном). При создании задачи можно запросить ИИ-прогноз оценки и увидеть, из чего он складывается.
- **Учёт времени** — списывайте часы вручную через модальное окно либо запускайте таймер автотрекинга на задаче, над которой работаете.
- **Язык интерфейса** — переключается в шапке (русский/английский).

## Документация

- [architecture.md](architecture.md) — архитектура и технические детали реализации: модель данных, REST API, бизнес-правила, устройство ML-прогноза, конфигурация.
- `TaskTracker/appsettings.example.json` и `.env.example` — шаблоны конфигурации с комментариями по каждому ключу.

## Контакты

Для вопросов и поддержки: Телеграм — @pofalor
