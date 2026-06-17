# Instrukcje dla agenta — brakujące elementy projektu Task Tracker

## 1. Stan obecny — co jest już zaimplementowane

| Wymaganie | Status | Szczegóły |
|---|---|---|
| #2 Kontener DI | ✅ Kompletne | ASP.NET Core DI we wszystkich 6 serwisach, `AddControllersWithApplicationAspects`, `AddSqlServerDbContextWithAudit` |
| #3 ORM | ✅ Kompletne | EF Core 9, SQL Server, migracje, `AuditFieldsSaveChangesInterceptor`, repozytoria |
| #4 Aspekty AOP | ✅ Kompletne | PostSharp: `LogExecutionAttribute` na kontrolerach, 4 aspekty biznesowe w TaskService, `LoggingBehavior` MediatR |
| #5 REST API | ✅ Kompletne | 6 serwisów z kontrolerami, YARP Gateway, Swagger w trybie Development |
| #6 Reaktywne strumienie | ✅ Kompletne | `TaskNotificationStream : IObservable<T>`, `TaskNotificationProcessor : IObserver<T>`, SignalR push przez hub |
| #7 Docker | ✅ Kompletne | `docker-compose.yml` z 9 kontenerami, indywidualne `Dockerfile` dla każdego serwisu |
| #8 Mikrousługi | ✅ Kompletne | UserService, ProjectService, TaskService, NotificationService, AuditService, ReportingService |
| **#1 Narzędzie budowania + testy + dokumentacja** | ❌ **BRAKUJE** | Brak skryptu budowania, brak jakichkolwiek projektów testowych, brak automatycznego generowania dokumentacji |

---

## 2. KRYTYCZNY BRAK — Wymaganie #1: Build tool, testy jednostkowe, dokumentacja

Wymaganie mówi dosłownie: _"narzędzie linii komend pozwalające na automatyzację procesu budowania aplikacji, w tym uruchamiania testów jednostkowych i generowania podstawowej dokumentacji"_

W projekcie **nie istnieje ani jeden projekt testowy**. Nie ma też żadnego skryptu automatyzującego build.

Należy zaimplementować trzy rzeczy:
1. Projekty testów jednostkowych (`xUnit` + `Moq` + `FluentAssertions`)
2. Skrypt CLI automatyzujący build, testy i generowanie dokumentacji (`build.ps1`)
3. Generowanie dokumentacji XML dla Swagger we wszystkich serwisach

---

## 3. ZADANIE A — Projekty testów jednostkowych

### 3.1 Struktura katalogów do utworzenia

```
Task_Tracker/
└── tests/
    ├── BuildingBlocks.Tests/
    │   └── BuildingBlocks.Tests.csproj
    ├── UserService.Tests/
    │   └── UserService.Tests.csproj
    ├── TaskService.Tests/
    │   └── TaskService.Tests.csproj
    └── ProjectService.Tests/
        └── ProjectService.Tests.csproj
```

### 3.2 Szablon pliku `.csproj` dla każdego projektu testowego

Każdy plik `.csproj` w katalogu `tests/` powinien wyglądać jak poniżej (dostosuj `ProjectReference` do testowanego serwisu):

**`tests/UserService.Tests/UserService.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\UserService\UserService.csproj" />
  </ItemGroup>
</Project>
```

**`tests/TaskService.Tests/TaskService.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\TaskService\TaskService.csproj" />
  </ItemGroup>
</Project>
```

**`tests/ProjectService.Tests/ProjectService.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\ProjectService\ProjectService.csproj" />
  </ItemGroup>
</Project>
```

**`tests/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.1" />
    <PackageReference Include="Microsoft.AspNetCore.Http" Version="2.2.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\BuildingBlocks\BuildingBlocks.csproj" />
  </ItemGroup>
</Project>
```

### 3.3 Dodanie projektów testowych do rozwiązania

Po utworzeniu plików `.csproj` wykonaj w terminalu (z katalogu `Task_Tracker/`):

```powershell
dotnet sln TaksTracker.sln add tests/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj
dotnet sln TaksTracker.sln add tests/UserService.Tests/UserService.Tests.csproj
dotnet sln TaksTracker.sln add tests/TaskService.Tests/TaskService.Tests.csproj
dotnet sln TaksTracker.sln add tests/ProjectService.Tests/ProjectService.Tests.csproj
```

### 3.4 Testy dla BuildingBlocks

Utwórz plik `tests/BuildingBlocks.Tests/Middleware/ApiExceptionHandlingMiddlewareTests.cs`:

```csharp
using BuildingBlocks.Middleware;
using BuildingBlocks.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BuildingBlocks.Tests.Middleware;

public class ApiExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndReturns200()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionHandlingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_Returns404()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionHandlingMiddleware(_ => throw new KeyNotFoundException("not found"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns403()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionHandlingMiddleware(_ => throw new UnauthorizedAccessException());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestValidationException_Returns400()
    {
        var errors = new[] { new RequestValidationError("Field", "Required") };
        var context = CreateContext();
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw new RequestValidationException(errors));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }
}
```

Utwórz plik `tests/BuildingBlocks.Tests/Validation/DataAnnotationsValidationBehaviorTests.cs`:

```csharp
using BuildingBlocks.Validation;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Tests.Validation;

public class DataAnnotationsValidationBehaviorTests
{
    private sealed record ValidRequest([property: Required][property: MinLength(3)] string Name);
    private sealed record EmptyRequest;

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new DataAnnotationsValidationBehavior<ValidRequest, Unit>(null!);
        var request = new ValidRequest("Test Name");
        var nextCalled = false;

        await behavior.Handle(request, () => { nextCalled = true; return Task.FromResult(Unit.Value); }, default);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsRequestValidationException()
    {
        var behavior = new DataAnnotationsValidationBehavior<ValidRequest, Unit>(null!);
        var request = new ValidRequest("AB"); // too short, MinLength(3)

        Func<Task> act = () => behavior.Handle(request,
            () => Task.FromResult(Unit.Value), default);

        await act.Should().ThrowAsync<RequestValidationException>()
            .Where(ex => ex.Errors.Any(e => e.Field == "Name"));
    }

    // Helper: dummy Unit type if MediatR.Unit is not directly accessible
    private sealed record Unit
    {
        public static readonly Unit Value = new();
    }
}
```

### 3.5 Testy dla UserService

Utwórz plik `tests/UserService.Tests/Features/RegisterUserHandlerTests.cs`:

```csharp
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Data;
using UserService.Entities;
using UserService.Features.Users;
using UserService.Messaging;
using UserService.Security;
using Microsoft.EntityFrameworkCore;

namespace UserService.Tests.Features;

public class RegisterUserHandlerTests
{
    private static UserDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthResponse()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var newUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@example.com", UserName = "test@example.com" };

        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => user.Id = newUser.Id);

        var tokenServiceMock = new Mock<IJwtTokenService>();
        tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns(new AuthResponse("fake-token", DateTime.UtcNow.AddHours(1)));

        var publisherMock = new Mock<IEventPublisher>();
        var dbContext = CreateInMemoryContext();
        var mapperMock = new Mock<IMapper>();

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            tokenServiceMock.Object,
            dbContext,
            publisherMock.Object,
            NullLogger<RegisterUserHandler>.Instance,
            mapperMock.Object);

        var command = new RegisterUserCommand("test@example.com", "Password123!", "Test User");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("fake-token");
    }

    [Fact]
    public async Task Handle_WhenUserManagerFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email taken" }));

        var tokenServiceMock = new Mock<IJwtTokenService>();
        var publisherMock = new Mock<IEventPublisher>();
        var dbContext = CreateInMemoryContext();
        var mapperMock = new Mock<IMapper>();

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            tokenServiceMock.Object,
            dbContext,
            publisherMock.Object,
            NullLogger<RegisterUserHandler>.Instance,
            mapperMock.Object);

        var command = new RegisterUserCommand("bad@example.com", "bad", "Bad");

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

Utwórz plik `tests/UserService.Tests/Security/JwtTokenServiceTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.Options;
using UserService.Entities;
using UserService.Security;

namespace UserService.Tests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var settings = Options.Create(new JwtSettings
        {
            Issuer = "tasktracker",
            Audience = "tasktracker",
            Key = "test-secret-key-must-be-32-chars!!",
            ExpiryMinutes = 60
        });
        return new JwtTokenService(settings);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@example.com", UserName = "test@example.com" };

        var result = service.GenerateToken(user, "User");

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_DifferentRoles_ReturnsDifferentTokens()
    {
        var service = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@example.com", UserName = "test@example.com" };

        var userToken = service.GenerateToken(user, "User");
        var adminToken = service.GenerateToken(user, "Admin");

        userToken.Token.Should().NotBe(adminToken.Token);
    }
}
```

### 3.6 Testy dla TaskService

Utwórz plik `tests/TaskService.Tests/Features/CreateTaskHandlerTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskService.Contracts;
using TaskService.Entities;
using TaskService.Features.Tasks;
using TaskService.Messaging;
using TaskService.Repositories;
using AutoMapper;
using TaskItemStatus = TaskService.Entities.TaskStatus;
using TaskItemPriority = TaskService.Entities.TaskPriority;

namespace TaskService.Tests.Features;

public class CreateTaskHandlerTests
{
    [Fact]
    public async Task Handle_AdminCreatesTaskWithAssignee_PublishesBothEvents()
    {
        // Arrange
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        var assigneeId = Guid.NewGuid().ToString();
        repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns(new TaskDto(Guid.NewGuid(), Guid.NewGuid(), "Test", "", assigneeId,
                TaskItemPriority.Medium, TaskItemStatus.Todo, null, DateTime.UtcNow));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            mapperMock.Object,
            NullLogger<CreateTaskHandler>.Instance);

        var command = new CreateTaskCommand(
            new CreateTaskRequest
            {
                Title = "Test Task",
                ProjectId = Guid.NewGuid(),
                AssigneeId = assigneeId,
                Priority = TaskItemPriority.Medium
            },
            UserId: "caller-id",
            IsAdmin: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        publisherMock.Verify(p => p.PublishAsync("tasks.created", It.IsAny<object>()), Times.Once);
        publisherMock.Verify(p => p.PublishAsync("tasks.assigned", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdminCreatesTask_AssigneeForcedToSelf()
    {
        // Arrange
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();
        var callerId = "caller-123";

        TaskItem? capturedTask = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => capturedTask = t)
            .Returns(Task.CompletedTask);

        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => new TaskDto(t.Id, t.ProjectId, t.Title, t.Description ?? "",
                t.AssigneeId ?? "", t.Priority, t.Status, t.DueDate, t.CreatedAt));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            mapperMock.Object,
            NullLogger<CreateTaskHandler>.Instance);

        var command = new CreateTaskCommand(
            new CreateTaskRequest
            {
                Title = "My Task",
                ProjectId = Guid.NewGuid(),
                AssigneeId = "some-other-user",
                Priority = TaskItemPriority.Low
            },
            UserId: callerId,
            IsAdmin: false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTask.Should().NotBeNull();
        capturedTask!.AssigneeId.Should().Be(callerId);
    }
}
```

Utwórz plik `tests/TaskService.Tests/Features/UpdateTaskHandlerTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskService.Contracts;
using TaskService.Entities;
using TaskService.Features.Tasks;
using TaskService.Messaging;
using TaskService.Repositories;
using AutoMapper;
using TaskItemStatus = TaskService.Entities.TaskStatus;
using TaskItemPriority = TaskService.Entities.TaskPriority;

namespace TaskService.Tests.Features;

public class UpdateTaskHandlerTests
{
    [Fact]
    public async Task Handle_NonAdminUpdatesOtherUserTask_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Other's Task",
                AssigneeId = "other-user",
                Status = TaskItemStatus.Todo,
                Priority = TaskItemPriority.Low
            });

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            new Mock<IMapper>().Object,
            NullLogger<UpdateTaskHandler>.Instance);

        var command = new UpdateTaskCommand(
            taskId,
            new UpdateTaskRequest { Title = "Hijack", Priority = TaskItemPriority.Low },
            UserId: "attacker-id",
            IsAdmin: false);

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_StatusChanged_PublishesStatusChangedEvent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var assigneeId = "assignee-1";
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        var existingTask = new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Task",
            AssigneeId = assigneeId,
            Status = TaskItemStatus.Todo,
            Priority = TaskItemPriority.Low
        };

        repoMock.Setup(r => r.GetAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns(new TaskDto(taskId, existingTask.ProjectId, "Task", "",
                assigneeId, TaskItemPriority.Low, TaskItemStatus.InProgress, null, DateTime.UtcNow));

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            mapperMock.Object,
            NullLogger<UpdateTaskHandler>.Instance);

        var command = new UpdateTaskCommand(
            taskId,
            new UpdateTaskRequest { Title = "Task", Status = TaskItemStatus.InProgress, Priority = TaskItemPriority.Low },
            UserId: assigneeId,
            IsAdmin: false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        publisherMock.Verify(p => p.PublishAsync("tasks.updated", It.IsAny<object>()), Times.Once);
        publisherMock.Verify(p => p.PublishAsync("tasks.statusChanged", It.IsAny<object>()), Times.Once);
    }
}
```

### 3.7 Testy dla ProjectService

Utwórz plik `tests/ProjectService.Tests/Features/CreateProjectHandlerTests.cs`:

```csharp
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectService.Contracts;
using ProjectService.Entities;
using ProjectService.Features.Projects;
using ProjectService.Messaging;
using ProjectService.Repositories;

namespace ProjectService.Tests.Features;

public class CreateProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProjectAndPublishesEvent()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();
        var ownerId = "owner-1";

        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto(Guid.NewGuid(), "Test Project", "", ownerId, DateTime.UtcNow, []));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            publisherMock.Object,
            mapperMock.Object,
            NullLogger<CreateProjectHandler>.Instance);

        var command = new CreateProjectCommand(
            new CreateProjectRequest { Name = "Test Project", Description = "Desc", MemberUserIds = [] },
            OwnerId: ownerId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Project");
        publisherMock.Verify(p => p.PublishAsync("projects.created", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CommandWithMembers_AddsInitialMembers()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        Project? capturedProject = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectDto(p.Id, p.Name, p.Description ?? "", p.OwnerId, p.CreatedAt,
                p.Members.Select(m => new ProjectMemberDto(m.Id, m.UserId, m.Role)).ToArray()));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            mapperMock.Object,
            NullLogger<CreateProjectHandler>.Instance);

        var memberIds = new[] { "user-1", "user-2" };
        var command = new CreateProjectCommand(
            new CreateProjectRequest { Name = "Team Project", MemberUserIds = memberIds },
            OwnerId: "owner-1");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProject.Should().NotBeNull();
        capturedProject!.Members.Should().HaveCount(2);
        capturedProject.Members.Select(m => m.UserId).Should().BeEquivalentTo(memberIds);
    }
}
```

Utwórz plik `tests/ProjectService.Tests/Features/ListProjectsHandlerTests.cs`:

```csharp
using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectService.Contracts;
using ProjectService.Entities;
using ProjectService.Features.Projects;
using ProjectService.Repositories;

namespace ProjectService.Tests.Features;

public class ListProjectsHandlerTests
{
    [Fact]
    public async Task Handle_AdminUser_ReturnsAllProjects()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var mapperMock = new Mock<IMapper>();

        var projects = new List<Project>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", OwnerId = "owner-1", Members = [] },
            new() { Id = Guid.NewGuid(), Name = "Beta", OwnerId = "owner-2", Members = [] }
        };

        repoMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        mapperMock.Setup(m => m.Map<IReadOnlyList<ProjectDto>>(It.IsAny<IEnumerable<Project>>()))
            .Returns((IEnumerable<Project> ps) => ps.Select(p =>
                new ProjectDto(p.Id, p.Name, "", p.OwnerId, p.CreatedAt, [])).ToList());

        var handler = new ListProjectsHandler(repoMock.Object, mapperMock.Object);
        var query = new ListProjectsQuery("admin-user", IsAdmin: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }
}
```

---

## 4. ZADANIE B — Skrypt automatyzujący build, testy i generowanie dokumentacji

### 4.1 Plik `build.ps1` (PowerShell — root projektu)

Utwórz plik `Task_Tracker/build.ps1`:

```powershell
<#
.SYNOPSIS
    Skrypt automatyzujący budowanie, testowanie i generowanie dokumentacji
    dla projektu Task Tracker.

.DESCRIPTION
    Uruchamia kolejno:
      1. Przywracanie zależności NuGet
      2. Budowanie rozwiązania
      3. Uruchamianie testów jednostkowych z raportem pokrycia
      4. Generowanie dokumentacji Swagger JSON dla każdego serwisu
      5. Kopiowanie wyników do katalogu artifacts/

.PARAMETER Target
    Cel budowania: All (domyślnie), Build, Test, Docs, Clean

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Target Test
    .\build.ps1 -Target Docs
    .\build.ps1 -Target Clean
#>

param(
    [ValidateSet("All", "Build", "Test", "Docs", "Clean")]
    [string]$Target = "All"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SolutionFile = "TaksTracker.sln"
$ArtifactsDir = "artifacts"
$DocsDir = "docs/swagger"
$TestResultsDir = "artifacts/test-results"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
}

function Invoke-Step([string]$StepName, [scriptblock]$Action) {
    Write-Step $StepName
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "Polecenie zakończyło się kodem błędu: $LASTEXITCODE"
        }
        Write-Host "✓ $StepName — OK" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ $StepName — BŁĄD: $_" -ForegroundColor Red
        exit 1
    }
}

function Invoke-Clean {
    Invoke-Step "Czyszczenie artefaktów" {
        if (Test-Path $ArtifactsDir) { Remove-Item -Recurse -Force $ArtifactsDir }
        dotnet clean $SolutionFile --nologo -v minimal
    }
}

function Invoke-Restore {
    Invoke-Step "Przywracanie pakietów NuGet" {
        dotnet restore $SolutionFile --nologo
    }
}

function Invoke-Build {
    Invoke-Step "Budowanie rozwiązania" {
        dotnet build $SolutionFile --no-restore --nologo -c Release
    }
}

function Invoke-Test {
    Invoke-Step "Uruchamianie testów jednostkowych" {
        New-Item -ItemType Directory -Force -Path $TestResultsDir | Out-Null

        dotnet test $SolutionFile `
            --no-build `
            --nologo `
            -c Release `
            --logger "trx;LogFileName=test-results.trx" `
            --results-directory $TestResultsDir `
            --verbosity normal

        Write-Host ""
        Write-Host "Wyniki testów zapisano w: $TestResultsDir" -ForegroundColor Yellow
    }
}

function Invoke-Docs {
    Invoke-Step "Generowanie dokumentacji Swagger JSON" {
        New-Item -ItemType Directory -Force -Path $DocsDir | Out-Null

        $services = @(
            @{ Name = "UserService";        Project = "src/Services/UserService/UserService.csproj";               Port = 5001 },
            @{ Name = "ProjectService";     Project = "src/Services/ProjectService/ProjectService.csproj";         Port = 5002 },
            @{ Name = "TaskService";        Project = "src/Services/TaskService/TaskService.csproj";               Port = 5003 },
            @{ Name = "NotificationService";Project = "src/Services/NotificationService/NotificationService.csproj";Port = 5004 },
            @{ Name = "AuditService";       Project = "src/Services/AuditService/AuditService.csproj";             Port = 5005 },
            @{ Name = "ReportingService";   Project = "src/Services/ReportingService/ReportingService.csproj";     Port = 5006 }
        )

        foreach ($svc in $services) {
            Write-Host "Generowanie Swagger dla: $($svc.Name)..." -ForegroundColor Yellow
            $outFile = "$DocsDir/$($svc.Name)-swagger.json"

            # Używa Swashbuckle CLI (dotnet-swagger) jeśli zainstalowane
            $toolCheck = dotnet tool list --global 2>&1 | Select-String "swashbuckle"
            if ($toolCheck) {
                dotnet swagger tofile `
                    --output $outFile `
                    "$($svc.Project.Replace('csproj',''))/bin/Release/net9.0/$($svc.Name).dll" `
                    v1
            } else {
                # Fallback: generuje dokumentację z XML docs w artifacts
                Write-Host "  Narzędzie swashbuckle CLI nie znalezione. Kopiowanie XML docs..." -ForegroundColor DarkYellow
                $xmlSource = "$($svc.Project.Replace('csproj',''))/bin/Release/net9.0/$($svc.Name).xml"
                if (Test-Path $xmlSource) {
                    Copy-Item $xmlSource "$DocsDir/$($svc.Name).xml"
                    Write-Host "  Skopiowano: $($svc.Name).xml" -ForegroundColor Green
                }
            }
        }

        Write-Host "Dokumentacja zapisana w: $DocsDir" -ForegroundColor Yellow
    }
}

# ──────────────────────────────────────────
# Główna logika
# ──────────────────────────────────────────

$startTime = Get-Date
Write-Host ""
Write-Host "Task Tracker — Build Script" -ForegroundColor Magenta
Write-Host "Cel: $Target  |  Czas startu: $startTime" -ForegroundColor Magenta

switch ($Target) {
    "Clean"  { Invoke-Clean }
    "Build"  { Invoke-Restore; Invoke-Build }
    "Test"   { Invoke-Restore; Invoke-Build; Invoke-Test }
    "Docs"   { Invoke-Restore; Invoke-Build; Invoke-Docs }
    "All"    { Invoke-Clean; Invoke-Restore; Invoke-Build; Invoke-Test; Invoke-Docs }
}

$elapsed = (Get-Date) - $startTime
Write-Host ""
Write-Host "✓ Zakończono [$Target] w $($elapsed.ToString('mm\:ss'))" -ForegroundColor Green
```

### 4.2 Plik `Makefile` (alternatywa cross-platform, root projektu)

Utwórz plik `Task_Tracker/Makefile`:

```makefile
# Task Tracker — Makefile
# Użycie: make [cel]
# Dostępne cele: all, build, test, docs, clean, docker-up, docker-down

SOLUTION     = TaksTracker.sln
ARTIFACTS    = artifacts
TEST_RESULTS = $(ARTIFACTS)/test-results
SWAGGER_DOCS = docs/swagger

.PHONY: all build test docs clean docker-up docker-down help

## Domyślny cel: buduj, testuj, generuj dokumentację
all: clean build test docs

## Przywróć zależności i zbuduj rozwiązanie
build:
	@echo ">>> Przywracanie pakietów NuGet..."
	dotnet restore $(SOLUTION) --nologo
	@echo ">>> Budowanie rozwiązania..."
	dotnet build $(SOLUTION) --no-restore --nologo -c Release

## Uruchom testy jednostkowe
test:
	@echo ">>> Uruchamianie testów jednostkowych..."
	@mkdir -p $(TEST_RESULTS)
	dotnet test $(SOLUTION) \
		--no-build \
		--nologo \
		-c Release \
		--logger "trx;LogFileName=test-results.trx" \
		--results-directory $(TEST_RESULTS) \
		--verbosity normal
	@echo ">>> Wyniki testów: $(TEST_RESULTS)"

## Zainstaluj narzędzie Swashbuckle CLI i wygeneruj Swagger JSON
docs:
	@echo ">>> Generowanie dokumentacji Swagger JSON..."
	@mkdir -p $(SWAGGER_DOCS)
	dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.9.0 2>/dev/null || true
	dotnet swagger tofile --output $(SWAGGER_DOCS)/UserService-swagger.json \
		src/Services/UserService/bin/Release/net9.0/UserService.dll v1 || true
	dotnet swagger tofile --output $(SWAGGER_DOCS)/ProjectService-swagger.json \
		src/Services/ProjectService/bin/Release/net9.0/ProjectService.dll v1 || true
	dotnet swagger tofile --output $(SWAGGER_DOCS)/TaskService-swagger.json \
		src/Services/TaskService/bin/Release/net9.0/TaskService.dll v1 || true
	dotnet swagger tofile --output $(SWAGGER_DOCS)/AuditService-swagger.json \
		src/Services/AuditService/bin/Release/net9.0/AuditService.dll v1 || true
	dotnet swagger tofile --output $(SWAGGER_DOCS)/ReportingService-swagger.json \
		src/Services/ReportingService/bin/Release/net9.0/ReportingService.dll v1 || true
	@echo ">>> Dokumentacja: $(SWAGGER_DOCS)"

## Usuń artefakty budowania
clean:
	@echo ">>> Czyszczenie..."
	dotnet clean $(SOLUTION) --nologo -v minimal
	@rm -rf $(ARTIFACTS)

## Uruchom wszystkie kontenery Docker
docker-up:
	docker compose up --build -d

## Zatrzymaj kontenery Docker
docker-down:
	docker compose down

## Wyświetl pomoc
help:
	@echo ""
	@echo "Task Tracker — dostępne cele:"
	@echo "  make all         — clean + build + test + docs"
	@echo "  make build       — restore + build"
	@echo "  make test        — uruchom testy jednostkowe"
	@echo "  make docs        — generuj Swagger JSON"
	@echo "  make clean       — usuń artefakty"
	@echo "  make docker-up   — uruchom Docker Compose"
	@echo "  make docker-down — zatrzymaj Docker Compose"
	@echo ""
```

---

## 5. ZADANIE C — Generowanie dokumentacji XML w serwisach

### 5.1 Włącz generowanie XML docs w każdym `.csproj`

W każdym z poniższych plików `.csproj` dodaj do sekcji `<PropertyGroup>`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

Pliki do edycji:
- `src/Services/UserService/UserService.csproj`
- `src/Services/TaskService/TaskService.csproj`
- `src/Services/ProjectService/ProjectService.csproj`
- `src/Services/NotificationService/NotificationService.csproj`
- `src/Services/AuditService/AuditService.csproj`
- `src/Services/ReportingService/ReportingService.csproj`

### 5.2 Konfiguracja Swagger z XML docs

Sprawdź, czy wszystkie serwisy mają Swagger skonfigurowany. W `UserService/Program.cs` jest już `AddSwaggerGen()` i `UseSwagger()`. Zweryfikuj, że pozostałe serwisy (ProjectService, TaskService, NotificationService, AuditService, ReportingService) mają identyczny fragment w swoim `Program.cs`:

```csharp
// W sekcji rejestracji serwisów:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "<NazwaSerwisu> API", Version = "v1" });

    // Dodaj XML docs do Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Konfiguracja JWT w Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Wpisz token JWT"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// W sekcji middleware pipeline (po var app = builder.Build()):
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "<NazwaSerwisu> API v1");
    });
}
```

### 5.3 Dodaj XML komentarze do kontrolerów

Wzorcowy przykład — dodaj komentarze `/// <summary>` do każdego kontrolera i akcji. Przykład dla `UserService/Controllers/AuthController.cs`:

```csharp
/// <summary>
/// Kontroler odpowiedzialny za uwierzytelnianie użytkowników.
/// </summary>
[ApiController]
[Route("api/users")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Rejestruje nowego użytkownika w systemie.
    /// </summary>
    /// <param name="command">Dane rejestracyjne: email, hasło, nazwa wyświetlana.</param>
    /// <returns>Token JWT oraz czas wygaśnięcia sesji.</returns>
    /// <response code="200">Rejestracja powiodła się — zwraca token JWT.</response>
    /// <response code="400">Dane rejestracyjne są nieprawidłowe.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command) { ... }

    /// <summary>
    /// Loguje istniejącego użytkownika i zwraca token JWT.
    /// </summary>
    /// <param name="command">Dane logowania: email i hasło.</param>
    /// <returns>Token JWT oraz czas wygaśnięcia sesji.</returns>
    /// <response code="200">Logowanie powiodło się — zwraca token JWT.</response>
    /// <response code="401">Nieprawidłowe dane logowania.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command) { ... }
}
```

Dodaj analogiczne komentarze do wszystkich kontrolerów we wszystkich serwisach.

---

## 6. ZADANIE D — Weryfikacja i uzupełnienie Swagger w pozostałych serwisach

Odczytaj `Program.cs` każdego serwisu (poza UserService, który jest już poprawnie skonfigurowany) i sprawdź, czy zawiera `AddSwaggerGen()` oraz `UseSwagger()`. Jeśli brakuje — dodaj zgodnie z wzorcem z sekcji 5.2.

Pliki do weryfikacji:
- `src/Services/TaskService/Program.cs`
- `src/Services/ProjectService/Program.cs`
- `src/Services/NotificationService/Program.cs`
- `src/Services/AuditService/Program.cs`
- `src/Services/ReportingService/Program.cs`

---

## 7. Kolejność wykonania

1. **Utwórz katalog `tests/`** z czterema projektami testowymi (sekcja 3.1–3.3)
2. **Utwórz pliki testów** (sekcje 3.4–3.7)
3. **Dodaj projekty do solucji** za pomocą `dotnet sln add ...` (sekcja 3.3)
4. **Edytuj każdy `.csproj` serwisu** dodając `GenerateDocumentationFile` (sekcja 5.1)
5. **Zweryfikuj i uzupełnij Swagger** we wszystkich serwisach (sekcje 5.2, 6)
6. **Dodaj XML komentarze** do kontrolerów (sekcja 5.3)
7. **Utwórz `build.ps1`** (sekcja 4.1)
8. **Utwórz `Makefile`** (sekcja 4.2)
9. **Weryfikacja**: uruchom `.\build.ps1` lub `make all` i sprawdź, czy testy przechodzą

---

## 8. Podsumowanie brakujących plików do utworzenia

| Plik | Opis |
|---|---|
| `tests/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj` | Projekt testowy dla BuildingBlocks |
| `tests/BuildingBlocks.Tests/Middleware/ApiExceptionHandlingMiddlewareTests.cs` | Testy middleware |
| `tests/BuildingBlocks.Tests/Validation/DataAnnotationsValidationBehaviorTests.cs` | Testy walidacji |
| `tests/UserService.Tests/UserService.Tests.csproj` | Projekt testowy dla UserService |
| `tests/UserService.Tests/Features/RegisterUserHandlerTests.cs` | Testy handlera rejestracji |
| `tests/UserService.Tests/Security/JwtTokenServiceTests.cs` | Testy generowania JWT |
| `tests/TaskService.Tests/TaskService.Tests.csproj` | Projekt testowy dla TaskService |
| `tests/TaskService.Tests/Features/CreateTaskHandlerTests.cs` | Testy tworzenia zadania |
| `tests/TaskService.Tests/Features/UpdateTaskHandlerTests.cs` | Testy edycji zadania |
| `tests/ProjectService.Tests/ProjectService.Tests.csproj` | Projekt testowy dla ProjectService |
| `tests/ProjectService.Tests/Features/CreateProjectHandlerTests.cs` | Testy tworzenia projektu |
| `tests/ProjectService.Tests/Features/ListProjectsHandlerTests.cs` | Testy listowania projektów |
| `build.ps1` | Skrypt PowerShell automatyzujący build+test+docs |
| `Makefile` | Alternatywny skrypt Makefile |

Pliki do modyfikacji:
| Plik | Zmiana |
|---|---|
| `src/Services/*/**.csproj` (6 plików) | Dodaj `<GenerateDocumentationFile>true</GenerateDocumentationFile>` |
| `src/Services/*/Program.cs` (5 plików bez UserService) | Zweryfikuj i dodaj Swagger z XML docs + JWT security definition |
| `src/Services/*/Controllers/*.cs` (wszystkie kontrolery) | Dodaj XML komentarze `/// <summary>` |
| `TaksTracker.sln` | Dodaj 4 projekty testowe przez `dotnet sln add` |
