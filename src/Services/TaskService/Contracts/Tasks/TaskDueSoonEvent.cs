namespace TaskService.Contracts.Tasks;

public sealed record TaskDueSoonEvent(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string AssigneeId,
    DateTime DueDate);
