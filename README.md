# Task Tracker — Platforma do zarządzania zadaniami i współpracy zespołowej

Aplikacja webowa oparta na architekturze mikrousług, umożliwiająca zarządzanie zadaniami w zespołach projektowych.

---

## Spis treści

1. [Architektura](#architektura)
2. [Wymagania](#wymagania)
3. [Szybki start (Docker)](#szybki-start-docker)
4. [Uruchomienie lokalne (bez Dockera)](#uruchomienie-lokalne)
5. [Skrypt budowania `build.ps1`](#skrypt-budowania)
6. [Testy jednostkowe](#testy-jednostkowe)
7. [Dokumentacja API (Swagger)](#dokumentacja-api)
8. [Weryfikacja wymagań projektowych](#weryfikacja-wymagan)
9. [Endpointy REST](#endpointy-rest)
10. [Zmienne konfiguracyjne](#zmienne-konfiguracyjne)

---

## Architektura

```
┌─────────────────────────────────────────────────────────┐
│         Frontend (React/Vite + Nginx)                   │
│         http://localhost:3000 (Docker)                  │
│         http://localhost:5173 (dev local)               │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│               YARP Gateway  :8080                       │
│           (reverse proxy + CORS + Consul)               │
└──┬──────┬──────┬──────┬──────┬──────┬───────────────────┘
   │      │      │      │      │      │
   ▼      ▼      ▼      ▼      ▼      ▼
User  Project  Task  Notif. Audit Report
Svc   Svc     Svc    Svc    Svc    Svc
   │      │      │      │      │      │
   └──────┴──┬───┴──────┴──────┴──────┘
             │  RabbitMQ (events bus)
             ▼
         SQL Server (osobna baza na serwis)
         Consul (service discovery)
```

### Serwisy

| Serwis | Port (Docker) | Opis |
|---|---|---|
| **Gateway** | 8080 | YARP reverse proxy — jeden punkt wejścia |
| **UserService** | wewnętrzny | Rejestracja, logowanie, JWT, zarządzanie profilami |
| **ProjectService** | wewnętrzny | CRUD projektów, zarządzanie członkami |
| **TaskService** | wewnętrzny | CRUD zadań, filtrowanie, priorytety, statusy |
| **NotificationService** | wewnętrzny | Powiadomienia push (SignalR + reaktywne strumienie) |
| **AuditService** | wewnętrzny | Historia wszystkich zdarzeń (konsument RabbitMQ) |
| **ReportingService** | wewnętrzny | Statystyki postępu projektów |
| **SQL Server** | 1433 | Baza danych (każdy serwis ma własną bazę) |
| **RabbitMQ** | 5672 / 15672 | Message broker (management UI na :15672) |
| **Consul** | 8500 | Service discovery |

---

## Wymagania

### Do uruchomienia przez Docker (zalecane)
- **Docker Desktop** 4.x+ z włączoną obsługą Linux containers
- **Docker Compose** v2+

### Do uruchomienia lokalnego / budowania
- **.NET 9 SDK** ([pobierz](https://aka.ms/dotnet/download))
- **SQL Server LocalDB** lub SQL Server Express
- **RabbitMQ** (lokalnie lub Docker)
- **Consul** (lokalnie lub Docker)
- **PowerShell 7+** (do skryptu `build.ps1`)

> **Uwaga o licencji PostSharp:** projekt używa PostSharp do aspektów AOP.  
> Bez licencji PostSharp build w trybie Release może wymagać ustawienia zmiennej środowiskowej:
> ```
> PostSharpLicense=<klucz>
> ```
> lub użycia `--no-warn:PostSharp` podczas budowania.  
> W Docker Compose klucz przekazywany jest przez ARG `PostSharpLicense` (domyślnie pusty — dla wersji próbnej lub projektów akademickich).

---

## Szybki start (Docker)

> **📖 Szczegółowa dokumentacja:** [docs/DOCKER_SETUP.md](docs/DOCKER_SETUP.md)

### 1. Sklonuj i wejdź do katalogu projektu

```bash
git clone <repo-url>
cd Task_Tracker
```

### 2. Uruchom wszystkie kontenery

```bash
docker compose up --build -d
```

Pierwsze uruchomienie pobiera obrazy i buduje serwisy — może potrwać kilka minut.

**Co zostanie uruchomione:**
- ✅ Frontend (React/Vite) → `http://localhost:3000`
- ✅ API Gateway (YARP) → `http://localhost:8080`
- ✅ 6 mikroservices (UserService, ProjectService, TaskService, NotificationService, AuditService, ReportingService)
- ✅ SQL Server → `localhost:1433`
- ✅ RabbitMQ → `localhost:5672` (management UI: `http://localhost:15672`)
- ✅ Consul → `http://localhost:8500`

### 3. Sprawdź status kontenerów

```bash
docker compose ps
```

Wszystkie serwisy powinny mieć status `running`. SQL Server potrzebuje ~30 sekund na start.

### 4. Otwórz aplikację w przeglądarce

**Frontend:** http://localhost:3000

**Domyślne konto administratora:**
- Email: `admin@tasktracker.local`
- Hasło: `Admin123!`

### 5. (Opcjonalnie) Testuj API bezpośrednio

```bash
# Poczekaj na gotowość Gateway
curl http://localhost:8080/health

# Logowanie przez API
curl -X POST http://localhost:8080/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tasktracker.local","password":"Admin123!"}'
```

Odpowiedź:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5...",
  "expiresAt": "2026-06-16T12:00:00Z"
}
```

### 6. Korzystaj z API

Użyj zwróconego tokena jako `Bearer <token>` w nagłówku `Authorization`.

### 7. Zatrzymaj kontenery

```bash
docker compose down
# lub z usunięciem danych (volumes):
docker compose down -v
```

---

## Uruchomienie lokalne

### Wymagane usługi zewnętrzne

Uruchom SQL Server, RabbitMQ i Consul lokalnie lub przez Docker:

```bash
# Tylko infrastruktura (bez serwisów aplikacyjnych)
docker compose up sqlserver rabbitmq consul -d
```

### Uruchomienie poszczególnych serwisów

Każdy serwis uruchamia się standardowo:

```bash
cd src/Services/UserService
dotnet run
```

### Uruchomienie frontendu (dev mode)

```bash
cd takstracker.client
npm install
npm run dev
```

Frontend będzie dostępny na: `http://localhost:5173`

**Uwaga:** W trybie development frontend łączy się z API na `http://127.0.0.1:8080` (wartość domyślna w `client.ts`).

Domyślne porty (z `appsettings.json`):
- UserService: `localhost:5001`
- ProjectService: `localhost:5002`
- TaskService: `localhost:5003`
- NotificationService: `localhost:5004`
- AuditService: `localhost:5005`
- ReportingService: `localhost:5006`
- Gateway: `localhost:5000`

---

## Skrypt budowania

Projekt zawiera skrypt PowerShell `build.ps1` automatyzujący cały proces.

### Dostępne cele

```powershell
# Pełny build (clean + restore + build + testy + dokumentacja)
.\build.ps1

# Tylko przywróć i zbuduj
.\build.ps1 -Target Build

# Tylko uruchom testy jednostkowe
.\build.ps1 -Target Test

# Tylko wygeneruj dokumentację Swagger
.\build.ps1 -Target Docs

# Wyczyść artefakty
.\build.ps1 -Target Clean
```

### Przykładowe wyjście

```
==================================================
  Budowanie rozwiązania (Release)
==================================================
✓ Budowanie rozwiązania (Release)

==================================================
  Uruchamianie testów jednostkowych
==================================================
Test run for BuildingBlocks.Tests.dll (.NETCoreApp,Version=v9.0)
  Passed: 10, Failed: 0, Skipped: 0
Test run for UserService.Tests.dll
  Passed: 6, Failed: 0, Skipped: 0
...
✓ Uruchamianie testów jednostkowych

Zakończono [All] w 02:15.30
```

### Makefile (Linux/macOS/WSL)

```bash
make all        # clean + build + test + docs
make build      # restore + build
make test       # testy jednostkowe
make docs       # Swagger JSON
make docker-up  # Docker Compose
```

---

## Testy jednostkowe

### Struktura testów

```
tests/
├── BuildingBlocks.Tests/     — testy shared library (middleware, walidacja)
│   ├── Middleware/           ApiExceptionHandlingMiddlewareTests (6 testów)
│   └── Validation/           DataAnnotationsValidationBehaviorTests (4 testy)
├── UserService.Tests/        — testy UserService
│   ├── Features/             RegisterUserHandlerTests (4 testy)
│   └── Security/             JwtTokenServiceTests (6 testów)
├── TaskService.Tests/        — testy TaskService
│   └── Features/             CreateTaskHandlerTests (4) + UpdateTaskHandlerTests (6)
└── ProjectService.Tests/     — testy ProjectService
    └── Features/             CreateProjectHandlerTests (4) + ListProjectsHandlerTests (3)
```

**Łącznie: 37 testów jednostkowych**

### Uruchomienie testów

```powershell
# Wszystkie testy naraz
dotnet test TaksTracker.sln --verbosity normal

# Konkretny projekt testowy
dotnet test tests/TaskService.Tests/TaskService.Tests.csproj -v normal

# Z raportem TRX
dotnet test TaksTracker.sln --logger "trx;LogFileName=results.trx" --results-directory artifacts/test-results
```

### Co jest testowane

| Projekt | Co testują |
|---|---|
| `BuildingBlocks.Tests` | Mapowanie wyjątków na kody HTTP (400/403/404/500), walidacja DataAnnotations przez MediatR pipeline |
| `UserService.Tests` | Rejestracja użytkownika (tworzenie profilu, publikacja eventu), generowanie i weryfikacja JWT (claims, role, issuer) |
| `TaskService.Tests` | Tworzenie zadań (reguła admin vs. non-admin), autoryzacja edycji, publikacja eventów statusChanged/assigned, odporność na błędy publishera |
| `ProjectService.Tests` | Tworzenie projektów z członkami, deduplikacja, publikacja eventu, filtrowanie projektów wg roli |

---

## Dokumentacja API

### Swagger UI

Po uruchomieniu projektu Swagger UI jest dostępny bezpośrednio na każdym serwisie:

| Serwis | URL Swagger UI |
|---|---|
| UserService | `http://localhost:5001/swagger` |
| ProjectService | `http://localhost:5002/swagger` |
| TaskService | `http://localhost:5003/swagger` |
| NotificationService | `http://localhost:5004/swagger` |
| AuditService | `http://localhost:5005/swagger` |
| ReportingService | `http://localhost:5006/swagger` |

W Swagger UI kliknij **Authorize** i wpisz token JWT uzyskany z endpointu `/api/users/login`.

### Generowanie Swagger JSON (offline)

```powershell
# Zainstaluj narzędzie Swashbuckle CLI
dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.9.0

# Wygeneruj JSON (po wcześniejszym build)
dotnet swagger tofile --output docs/swagger/UserService-swagger.json \
  src/Services/UserService/bin/Release/net9.0/UserService.dll v1
```

Lub użyj skryptu:
```powershell
.\build.ps1 -Target Docs
```

---

## Weryfikacja wymagań

### Wymaganie #1 — Narzędzie budowania ✅

| Komponent | Plik | Opis |
|---|---|---|
| Skrypt CLI | `build.ps1` | PowerShell: `.\build.ps1 -Target [All|Build|Test|Docs|Clean]` |
| Makefile | `Makefile` | `make [all|build|test|docs|clean|docker-up]` |
| Testy jednostkowe | `tests/` | 37 testów xUnit + Moq + FluentAssertions |
| Dokumentacja XML | `src/Services/*/*.csproj` | `<GenerateDocumentationFile>true` |
| Swagger JSON | `docs/swagger/*.json` | Generowane przez Swashbuckle CLI |

```powershell
# Weryfikacja — uruchom kompletny build z testami:
.\build.ps1 -Target All
```

### Wymaganie #2 — Kontener DI ✅

ASP.NET Core DI we wszystkich 6 serwisach. Kluczowe rejestracje:

```csharp
// BuildingBlocks — wspólne aspekty i filtry
builder.Services.AddControllersWithApplicationAspects();

// EF Core z interceptorem audytu
builder.Services.AddSqlServerDbContextWithAudit<UserDbContext>(configuration);

// Rejestracja repozytorium
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Serwisy domenowe
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Singleton dla RabbitMQ publisher
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// BackgroundService (Hosted Service)
builder.Services.AddHostedService<ConsulRegistrationService>();
builder.Services.AddHostedService<RabbitMqEventConsumer>();
```

**Jak sprawdzić:** uruchom `docker compose up` i wywołaj dowolny endpoint — DI rozwiązuje zależności automatycznie.

### Wymaganie #3 — ORM (EF Core) ✅

- Każdy serwis ma własny `DbContext` z osobną bazą danych SQL Server
- Migracje aplikowane automatycznie przy starcie (`Database.Migrate()`)
- `AuditFieldsSaveChangesInterceptor` — automatyczne znaczniki `CreatedAt`/`UpdatedAt`
- Repozytoria (`ITaskRepository`, `IProjectRepository`, itp.) enkapsulują dostęp do danych

```bash
# Weryfikacja — sprawdź migracje w SQL Server po starcie Dockera:
# Każda baza: UserServiceDb, ProjectServiceDb, TaskServiceDb, ...
```

### Wymaganie #4 — Aspekty AOP (PostSharp) ✅

| Aspekt | Gdzie | Co robi |
|---|---|---|
| `[LogExecution]` | Wszystkie kontrolery | Loguje wejście/wyjście/czas każdej akcji HTTP |
| `[NotifyOverdueTasks]` | `TasksController` | Po każdym GET/POST/PUT/DELETE sprawdza przeterminowane zadania i publishuje `tasks.overdue` |
| `[NotifyUpcomingTaskDeadlines]` | `TasksController` | Wykrywa zadania z terminem < 24h |
| `[NotifyHighPriorityTasks]` | `TasksController` | Wykrywa zadania o wysokim priorytecie bez powiadomienia |
| `[RefreshProjectReport]` | `TasksController` (mutacje) | Po zmianie zadania aktualizuje statystyki projektu |
| `LoggingBehavior<TReq,TRes>` | MediatR pipeline | Loguje każdy handler MediatR |
| `NullRequestActionFilter` | Wszystkie kontrolery | Zwraca 400 gdy body requestu jest null |

```bash
# Weryfikacja — wywołaj endpoint i sprawdź logi kontenera:
docker compose logs taskservice | grep "Executing\|Executed"
```

### Wymaganie #5 — REST API ✅

Wszystkie funkcjonalności dostępne przez REST. Gateway na porcie `8080` routuje do serwisów:

```
GET    /api/users            → UserService
POST   /api/users/register   → UserService
POST   /api/users/login      → UserService
GET    /api/projects         → ProjectService
POST   /api/projects         → ProjectService
GET    /api/tasks            → TaskService
POST   /api/tasks            → TaskService
GET    /api/notifications    → NotificationService
GET    /api/audit            → AuditService
GET    /api/reports          → ReportingService
```

```bash
# Weryfikacja — przykładowe wywołanie przez Gateway:
TOKEN=$(curl -s -X POST http://localhost:8080/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tasktracker.local","password":"Admin123!"}' \
  | jq -r '.token')

curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/projects
```

### Wymaganie #6 — Reaktywne strumienie ✅

Implementacja w `NotificationService`:

```csharp
// TaskNotificationStream : IObservable<TaskNotificationEvent>
// — własna implementacja wzorca Obserwator bez zewnętrznych bibliotek

// RabbitMqEventConsumer — odbiera zdarzenia z RabbitMQ i pushuje do strumienia:
stream.Publish(notification); // IObservable.OnNext()

// TaskNotificationProcessor : BackgroundService — subskrybuje strumień:
subscription = stream.Subscribe(new Observer(notification =>
    _ = PersistAndBroadcastAsync(notification, stoppingToken)));

// Po przetworzeniu — push przez SignalR do klienta:
await hubContext.Clients.Group(notification.UserId)
    .SendAsync("notification", payload);
```

```bash
# Weryfikacja — połącz się przez SignalR i obserwuj powiadomienia:
# Hub: ws://localhost:8080/hubs/notifications
# (wymaga Bearer tokena w query string lub nagłówku)

# Utwórz zadanie i obserwuj event w logach:
docker compose logs notificationservice | grep "notification"
```

### Wymaganie #7 — Docker ✅

```bash
# Uruchomienie całości
docker compose up --build -d

# Lista kontenerów
docker compose ps

# Logi konkretnego serwisu
docker compose logs -f taskservice

# Zatrzymanie z usunięciem danych
docker compose down -v
```

**Co zawiera `docker-compose.yml`:**
- **10 kontenerów:** Frontend (React/Vite + Nginx) + 6 mikroservices + Gateway + SQL Server + RabbitMQ + Consul
- Każdy serwis ma własny `Dockerfile` (multi-stage build)
- Frontend buildowany przez Node.js i serwowany przez Nginx
- Health checks dla SQL Server (TCP probe)
- Persistent volume dla SQL Server
- Restart policy: `on-failure`

**Porty:**
- Frontend: `http://localhost:3000`
- API Gateway: `http://localhost:8080`
- SQL Server: `localhost:1433`
- RabbitMQ: `localhost:5672` (management: `15672`)
- Consul: `http://localhost:8500`

### Wymaganie #8 — Mikrousługi ✅

| Kryterium | Realizacja |
|---|---|
| Osobne procesy | 6 niezależnych projektów ASP.NET Core Web API |
| Osobne bazy danych | Każdy serwis ma własną bazę SQL Server |
| Komunikacja async | RabbitMQ topic exchange `tasktracker.events` |
| Service discovery | Consul — każdy serwis rejestruje się przy starcie |
| API Gateway | YARP `src/Gateway` — jeden punkt wejścia |
| Izolacja konfiguracji | Osobne `appsettings.json` i zmienne środowiskowe |

---

## Endpointy REST

### Uwierzytelnianie

```
POST /api/users/register    — rejestracja nowego użytkownika
POST /api/users/login       — logowanie, zwraca JWT
```

### Użytkownicy (wymaga JWT)

```
GET    /api/users           — lista wszystkich użytkowników
GET    /api/users/me        — profil aktualnego użytkownika
PUT    /api/users/me        — aktualizacja profilu
PUT    /api/users/me/password               — zmiana hasła
PUT    /api/users/{userId}/role    [Admin]  — zmiana roli
PUT    /api/users/{userId}/password [Admin] — reset hasła
```

### Projekty (wymaga JWT)

```
GET    /api/projects                           — lista projektów
GET    /api/projects/{id}                      — szczegóły projektu
POST   /api/projects                  [Admin]  — utwórz projekt
PUT    /api/projects/{id}             [Admin]  — edytuj projekt
DELETE /api/projects/{id}             [Admin]  — usuń projekt
POST   /api/projects/{id}/members     [Admin]  — dodaj członka
```

### Zadania (wymaga JWT)

```
GET    /api/tasks                              — lista zadań (filtry: ?projectId=&assigneeId=&status=&priority=&search=)
GET    /api/tasks/{id}                         — szczegóły zadania
POST   /api/tasks                              — utwórz zadanie
PUT    /api/tasks/{id}                         — edytuj zadanie
DELETE /api/tasks/{id}                         — usuń zadanie
```

**Statusy zadań:** `Todo(0)`, `InProgress(1)`, `Done(2)`, `Blocked(3)`  
**Priorytety:** `Low(0)`, `Medium(1)`, `High(2)`, `Critical(3)`

### Powiadomienia (wymaga JWT)

```
GET /api/notifications              — lista powiadomień (opcjonalnie ?projectId=)
PUT /api/notifications/{id}         — oznacz jako przeczytane/nieprzeczytane
WS  /hubs/notifications             — SignalR hub (push w czasie rzeczywistym)
```

### Audyt (wymaga JWT)

```
GET /api/audit                      — logi audytu (?userId=&action=&entityType=)
```

### Raporty (wymaga JWT)

```
GET  /api/reports                   — raporty wszystkich projektów
GET  /api/reports/{projectId}       — raport konkretnego projektu
POST /api/reports                   — utwórz/aktualizuj raport
```

---

## Zmienne konfiguracyjne

### appsettings.json (wspólne dla wszystkich serwisów)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=<ServiceName>Db;..."
  },
  "Jwt": {
    "Issuer": "tasktracker",
    "Audience": "tasktracker",
    "Key": "tasktracker-development-secret-key-32chars",
    "ExpiryMinutes": 60
  },
  "RabbitMq": {
    "Host": "rabbitmq",
    "Exchange": "tasktracker.events",
    "Queue": "<service-queue>"
  },
  "Consul": {
    "ConsulAddress": "http://consul:8500",
    "ServiceName": "<servicename>",
    "ServiceId": "<servicename>-1",
    "ServiceAddress": "http://<servicename>:8080"
  }
}
```

### Konto administratora (UserService)

```json
"AdminSeed": {
  "Email": "admin@tasktracker.local",
  "Password": "Admin123!",
  "DisplayName": "Administrator"
}
```

---

## Struktura katalogów

```
Task_Tracker/
├── build.ps1                  ← skrypt budowania (PowerShell)
├── Makefile                   ← alternatywa dla make
├── docker-compose.yml         ← definicja całego stacku
├── TaksTracker.sln            ← solucja Visual Studio
├── src/
│   ├── Gateway/               ← YARP API Gateway
│   ├── BuildingBlocks/        ← wspólna biblioteka (aspekty, middleware, persistence)
│   └── Services/
│       ├── UserService/
│       ├── ProjectService/
│       ├── TaskService/
│       ├── NotificationService/
│       ├── AuditService/
│       └── ReportingService/
├── tests/
│   ├── BuildingBlocks.Tests/
│   ├── UserService.Tests/
│   ├── TaskService.Tests/
│   └── ProjectService.Tests/
└── docs/
    ├── csharp-mechanisms.md   ← opis mechanizmów C#/.NET
    └── swagger/               ← generowane pliki Swagger JSON
```
