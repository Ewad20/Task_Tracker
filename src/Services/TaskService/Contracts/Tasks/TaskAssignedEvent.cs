namespace TaskService.Contracts.Tasks;

public sealed record TaskAssignedEvent(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string AssigneeId,
    string? PreviousAssigneeId);
