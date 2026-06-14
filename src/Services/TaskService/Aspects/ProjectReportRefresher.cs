using Microsoft.EntityFrameworkCore;
using TaskService.Contracts.Tasks;
using TaskService.Data;
using TaskService.Messaging;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Aspects;

public sealed class ProjectReportRefresher(
    TaskDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<ProjectReportRefresher> logger) : IProjectReportRefresher
{
    public async Task<Guid?> ResolveProjectIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await dbContext.Tasks
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task PublishReportUpdateAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var stats = await dbContext.Tasks
            .Where(task => task.ProjectId == projectId)
            .GroupBy(task => task.ProjectId)
            .Select(group => new
            {
                TotalTasks = group.Count(),
                CompletedTasks = group.Count(task => task.Status == TaskStatus.Done)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var payload = new ProjectReportStatsChangedEvent(
            projectId,
            stats?.TotalTasks ?? 0,
            stats?.CompletedTasks ?? 0);

        try
        {
            eventPublisher.Publish("reports.projectStatsChanged", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish project report update for project {ProjectId}.", projectId);
        }
    }
}
