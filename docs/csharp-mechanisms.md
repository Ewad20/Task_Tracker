# Mechanizmy C#/.NET uzyte w projekcie

Projekt wykorzystuje mechanizmy omawiane na wykladach w nastepujacych miejscach:

- Programowanie aspektowe w stylu PostSharp:
  - `BuildingBlocks.Aspects.LogExecutionAttribute` dziedziczy po `MethodInterceptionAspect`, wiec przechwytuje wykonanie kontrolerow przez klasyczny PostSharp weaving podczas builda.
  - `TaskService.Aspects.NotifyOverdueTasksAttribute` jest aspektem biznesowym, ktory po wykonaniu akcji zadan sprawdza przekroczone terminy i publikuje zdarzenia powiadomien.
  - `TaskService.Aspects.NotifyUpcomingTaskDeadlinesAttribute` jest aspektem biznesowym, ktory wykrywa zadania z terminem w ciagu 24 godzin i publikuje powiadomienia `tasks.dueSoon`.
  - `TaskService.Aspects.NotifyHighPriorityTasksAttribute` jest aspektem biznesowym, ktory wykrywa zadania wysokiego priorytetu i publikuje powiadomienia `tasks.highPriority`.
  - `TaskService.Aspects.RefreshProjectReportAttribute` jest aspektem biznesowym, ktory po utworzeniu, edycji lub usunieciu zadania publikuje aktualne statystyki projektu dla `ReportingService`.
  - `BuildingBlocks.Aspects.LoggingBehavior` przechwytuje wykonanie handlerow MediatR.
  - `BuildingBlocks.Aspects.NullRequestActionFilter` blokuje wykonanie akcji, gdy zlozony request jest pusty.
- Atrybuty C#:
  - kontrolery sa oznaczone `[LogExecution(...)]`,
  - requesty uzywaja `[Required]`, `[StringLength]`, `[MinLength]`, `[EmailAddress]` i `[Range]`.
- Wstrzykiwanie zaleznosci:
  - kazdy mikroserwis rejestruje zaleznosci przez `builder.Services`,
  - wspolne aspekty sa rejestrowane przez `AddControllersWithApplicationAspects`,
  - konteksty EF Core sa rejestrowane przez `AddSqlServerDbContextWithAudit<TContext>`.
- Host aplikacji:
  - mikroserwisy uzywaja `WebApplication.CreateBuilder(args)`,
  - uslugi dzialajace w tle sa rejestrowane przez `AddHostedService`, np. Consul i RabbitMQ.
- Entity Framework Core:
  - serwisy maja wlasne klasy `DbContext`,
  - dane sa mapowane przez encje i `DbSet`,
  - migracje pozostaja w katalogach `Migrations`,
  - `Database.Migrate()` stosuje migracje przy starcie,
  - `AuditFieldsSaveChangesInterceptor` ustawia pola `CreatedAt` i `UpdatedAt` przed zapisem.
- Obsluga wyjatkow:
  - `ApiExceptionHandlingMiddleware` centralnie mapuje wyjatki na odpowiedzi HTTP,
  - bledy walidacji zwracaja status `400`,
  - brak zasobu zwraca status `404`.

Projekt ma dodana zaleznosc od `PostSharp` w `BuildingBlocks` oraz mikroserwisach, w ktorych stosowany jest `[LogExecution]`. Do pelnego builda z weavingiem potrzebna jest aktywna licencja PostSharp.
