using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Contracts.Tasks;

public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string AssigneeId,
    TaskStatus PreviousStatus,
    TaskStatus Status);
