using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskService.Contracts.Tasks;
using TaskService.Entities;
using TaskService.Features.Tasks;
using TaskService.Messaging;
using TaskService.Repositories;
using TaskItemStatus = TaskService.Entities.TaskStatus;
using TaskItemPriority = TaskService.Entities.TaskPriority;

namespace TaskService.Tests.Features;

public class CreateTaskHandlerTests
{
    private static TaskDto MakeDto(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Title, t.Description ?? "",
            t.AssigneeId ?? "", t.Priority, t.Status, t.DueDate, t.CreatedAt);

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
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateTaskHandler>.Instance,
            mapperMock.Object);

        var request = new CreateTaskRequest(
            Guid.NewGuid(), "Test Task", "", assigneeId, TaskItemPriority.Medium, null);
        var command = new CreateTaskCommand(request, "admin-user", true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Task");

        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("tasks.created");
        publishCalls.Should().Contain("tasks.assigned");
    }

    [Fact]
    public async Task Handle_NonAdminCreatesTask_AssigneeIdForcedToCurrentUser()
    {
        // Arrange
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();
        var callerId = "caller-user-123";
        TaskItem? capturedTask = null;

        repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => capturedTask = t)
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateTaskHandler>.Instance,
            mapperMock.Object);

        var request = new CreateTaskRequest(
            Guid.NewGuid(), "My Task", "", "some-other-user", TaskItemPriority.Low, null);
        var command = new CreateTaskCommand(request, callerId, false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTask.Should().NotBeNull();
        capturedTask!.AssigneeId.Should().Be(callerId,
            "non-admin users cannot assign tasks to other users");
    }

    [Fact]
    public async Task Handle_AdminCreatesTaskWithoutAssignee_PublishesOnlyCreatedEvent()
    {
        // Arrange
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateTaskHandler>.Instance,
            mapperMock.Object);

        // AssigneeId is empty string
        var request = new CreateTaskRequest(
            Guid.NewGuid(), "Unassigned Task", "", "", TaskItemPriority.Medium, null);
        var command = new CreateTaskCommand(request, "admin-user", true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("tasks.created");
        publishCalls.Should().NotContain("tasks.assigned",
            "no assigned event should be sent when AssigneeId is empty");
    }

    [Fact]
    public async Task Handle_PublisherThrows_HandlerStillSucceeds()
    {
        // Arrange
        var repoMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));
        publisherMock.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<object>()))
            .Throws(new Exception("RabbitMQ unavailable"));

        var handler = new CreateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateTaskHandler>.Instance,
            mapperMock.Object);

        var request = new CreateTaskRequest(
            Guid.NewGuid(), "Task", "", "user", TaskItemPriority.Low, null);
        var command = new CreateTaskCommand(request, "admin", true);

        // Act & Assert — must not throw even when publisher fails
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
