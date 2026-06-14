using Microsoft.EntityFrameworkCore;
using TaskService.Contracts.Tasks;
using TaskService.Data;
using TaskService.Entities;
using TaskService.Messaging;

namespace TaskService.Aspects;

public sealed class HighPriorityTaskNotifier(
    TaskDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<HighPriorityTaskNotifier> logger) : IHighPriorityTaskNotifier
{
    public async Task NotifyHighPriorityTasksAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var highPriorityTasks = await dbContext.Tasks
            .Where(task => task.Priority == TaskPriority.High
                && task.Status != Entities.TaskStatus.Done
                && task.HighPriorityNotificationSentAt == null
                && task.AssigneeId != string.Empty)
            .ToListAsync(cancellationToken);

        foreach (var task in highPriorityTasks)
        {
            try
            {
                eventPublisher.Publish(
                    "tasks.highPriority",
                    new TaskHighPriorityEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        task.Priority));

                task.HighPriorityNotificationSentAt = now;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish high priority notification for task {TaskId}.", task.Id);
            }
        }

        if (highPriorityTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
