using Microsoft.EntityFrameworkCore;
using TaskService.Contracts.Tasks;
using TaskService.Data;
using TaskService.Messaging;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Aspects;

public sealed class UpcomingTaskDeadlineNotifier(
    TaskDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<UpcomingTaskDeadlineNotifier> logger) : IUpcomingTaskDeadlineNotifier
{
    public async Task NotifyUpcomingDeadlinesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddHours(24);
        var dueSoonTasks = await dbContext.Tasks
            .Where(task => task.DueDate.HasValue
                && task.DueDate.Value >= now
                && task.DueDate.Value <= threshold
                && task.Status != TaskStatus.Done
                && task.DueSoonNotificationSentAt == null
                && task.AssigneeId != string.Empty)
            .ToListAsync(cancellationToken);

        foreach (var task in dueSoonTasks)
        {
            try
            {
                eventPublisher.Publish(
                    "tasks.dueSoon",
                    new TaskDueSoonEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        task.DueDate!.Value));

                task.DueSoonNotificationSentAt = now;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish upcoming deadline notification for task {TaskId}.", task.Id);
            }
        }

        if (dueSoonTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
