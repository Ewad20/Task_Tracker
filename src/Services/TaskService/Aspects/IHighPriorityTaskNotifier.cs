namespace TaskService.Aspects;

public interface IHighPriorityTaskNotifier
{
    Task NotifyHighPriorityTasksAsync(CancellationToken cancellationToken);
}
