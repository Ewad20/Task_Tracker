namespace ReportingService.Entities;

public sealed class ProjectReport
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double ProgressPercent { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
