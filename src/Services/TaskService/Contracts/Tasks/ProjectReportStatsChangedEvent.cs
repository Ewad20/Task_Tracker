namespace TaskService.Contracts.Tasks;

public sealed record ProjectReportStatsChangedEvent(
    Guid ProjectId,
    int TotalTasks,
    int CompletedTasks);
