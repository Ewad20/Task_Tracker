using System.ComponentModel.DataAnnotations;

namespace ReportingService.Contracts.Reports;

public sealed record UpsertProjectReportRequest(
    [Required] Guid ProjectId,
    [Range(0, int.MaxValue)] int TotalTasks,
    [Range(0, int.MaxValue)] int CompletedTasks);
