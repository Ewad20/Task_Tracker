using TaskPriority = TaskService.Entities.TaskPriority;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Contracts.Tasks;

public sealed record TaskFilterRequest(
    Guid? ProjectId,
    string? AssigneeId,
    TaskStatus? Status,
    TaskPriority? Priority,
    string? Search);
