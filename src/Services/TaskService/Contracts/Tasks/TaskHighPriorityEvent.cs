using TaskPriority = TaskService.Entities.TaskPriority;

namespace TaskService.Contracts.Tasks;

public sealed record TaskHighPriorityEvent(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string AssigneeId,
    TaskPriority Priority);
