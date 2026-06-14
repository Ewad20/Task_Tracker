using Microsoft.EntityFrameworkCore;
using TaskService.Contracts.Tasks;
using TaskService.Data;
using TaskService.Entities;
using TaskService.Messaging;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Aspects;

public sealed class OverdueTaskNotifier(
    TaskDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<OverdueTaskNotifier> logger) : IOverdueTaskNotifier
{
    public async Task NotifyOverdueTasksAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var overdueTasks = await dbContext.Tasks
            .Where(task => task.DueDate.HasValue
                && task.DueDate.Value < now
                && task.Status != TaskStatus.Done
                && task.OverdueNotificationSentAt == null
                && task.AssigneeId != string.Empty)
            .ToListAsync(cancellationToken);

        foreach (var task in overdueTasks)
        {
            try
            {
                eventPublisher.Publish(
                    "tasks.overdue",
                    new TaskOverdueEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        task.DueDate!.Value));

                task.OverdueNotificationSentAt = now;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish overdue notification for task {TaskId}.", task.Id);
            }
        }

        if (overdueTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
