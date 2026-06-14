# Task Tracker Microservices

Ten projekt zawiera implementację aplikacji webowej do zarządzania zadaniami i współpracy zespołowej w architekturze mikrousług.

## Struktura katalogów

```
src/
  Gateway/
  Services/
    UserService/
    ProjectService/
    TaskService/
    NotificationService/
    AuditService/
    ReportingService/
infra/
```

## Uruchomienie

1. Zainstaluj Docker + Docker Compose.
2. Uruchom:

```
docker compose up --build
```

Gateway jest dostępny pod `http://localhost:8080`.

## Technologie

- .NET 9
- SQL Server Express + Entity Framework Core
- YARP API Gateway
- Consul (service discovery)
- ASP.NET Identity + JWT
- RabbitMQ
- Docker + Docker Compose
- OpenTelemetry + Health Checks
- Swagger / Swashbuckle

## Usługi

- User Service (`/api/users`)
- Project Service (`/api/projects`)
- Task Service (`/api/tasks`)
- Notification Service (`/api/notifications`, SignalR `/hubs/notifications`)
- Audit Service (`/api/audit`)
- Reporting Service (`/api/reports`)

Każda usługa posiada własną bazę danych, przykładowe encje, DTO, CQRS/MediatR, AutoMapper oraz globalną obsługę wyjątków.
