namespace ReportingService.Contracts.Reports;

public sealed record ProjectReportDto(Guid Id, Guid ProjectId, int TotalTasks, int CompletedTasks, double ProgressPercent, DateTime UpdatedAt);
