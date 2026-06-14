namespace TaskService.Contracts.Tasks;

public sealed record TaskOverdueEvent(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string AssigneeId,
    DateTime DueDate);
