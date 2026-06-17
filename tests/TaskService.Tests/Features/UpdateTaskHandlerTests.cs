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

public class UpdateTaskHandlerTests
{
    private static TaskDto MakeDto(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Title, t.Description ?? "",
            t.AssigneeId ?? "", t.Priority, t.Status, t.DueDate, t.CreatedAt);

    private static TaskItem MakeTask(string assigneeId = "owner-1",
        TaskItemStatus status = TaskItemStatus.Todo,
        TaskItemPriority priority = TaskItemPriority.Low) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = "Existing Task",
            AssigneeId = assigneeId,
            Status = status,
            Priority = priority
        };

    [Fact]
    public async Task Handle_NonAdminUpdatesOwnTask_Succeeds()
    {
        // Arrange
        var task = MakeTask(assigneeId: "user-1");
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<UpdateTaskHandler>.Instance,
            mapperMock.Object);

        var request = new UpdateTaskRequest(
            "Updated Title", "", "user-1", TaskItemPriority.Low, TaskItemStatus.Todo, null);
        var command = new UpdateTaskCommand(task.Id, request, "user-1", false);

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_NonAdminUpdatesOtherUserTask_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var task = MakeTask(assigneeId: "other-user");
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<UpdateTaskHandler>.Instance,
            new Mock<IMapper>().Object);

        var request = new UpdateTaskRequest(
            "Hijacked Title", "", "other-user", TaskItemPriority.Low, TaskItemStatus.Todo, null);
        var command = new UpdateTaskCommand(task.Id, request, "attacker-id", false);

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyTask_Succeeds()
    {
        // Arrange
        var task = MakeTask(assigneeId: "some-user");
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<UpdateTaskHandler>.Instance,
            mapperMock.Object);

        var request = new UpdateTaskRequest(
            "Admin Edit", "", "some-user", TaskItemPriority.Low, TaskItemStatus.Todo, null);
        var command = new UpdateTaskCommand(task.Id, request, "admin-user", true);

        // Act & Assert
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_StatusChanged_PublishesStatusChangedEvent()
    {
        // Arrange
        var task = MakeTask(assigneeId: "user-1", status: TaskItemStatus.Todo);
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<UpdateTaskHandler>.Instance,
            mapperMock.Object);

        // Change status from Todo -> InProgress
        var request = new UpdateTaskRequest(
            task.Title, "", "user-1", TaskItemPriority.Low, TaskItemStatus.InProgress, null);
        var command = new UpdateTaskCommand(task.Id, request, "user-1", false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("tasks.updated");
        publishCalls.Should().Contain("tasks.statusChanged");
    }

    [Fact]
    public async Task Handle_AssigneeChanged_PublishesAssignedEvent()
    {
        // Arrange
        var task = MakeTask(assigneeId: "user-1");
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
            .Returns((TaskItem t) => MakeDto(t));

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<UpdateTaskHandler>.Instance,
            mapperMock.Object);

        // Admin changes assignee to new user
        var request = new UpdateTaskRequest(
            task.Title, "", "user-2", TaskItemPriority.Low, TaskItemStatus.Todo, null);
        var command = new UpdateTaskCommand(task.Id, request, "admin", true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("tasks.assigned");
    }

    [Fact]
    public async Task Handle_TaskNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var repoMock = new Mock<ITaskRepository>();
        repoMock.Setup(r => r.GetAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var handler = new UpdateTaskHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<UpdateTaskHandler>.Instance,
            new Mock<IMapper>().Object);

        var request = new UpdateTaskRequest(
            "Title", "", "user", TaskItemPriority.Low, TaskItemStatus.Todo, null);
        var command = new UpdateTaskCommand(taskId, request, "user", false);

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
