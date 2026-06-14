using TaskPriority = TaskService.Entities.TaskPriority;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Contracts.Tasks;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Description,
    string AssigneeId,
    TaskPriority Priority,
    TaskStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt);
