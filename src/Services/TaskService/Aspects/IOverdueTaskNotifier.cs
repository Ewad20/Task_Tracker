namespace TaskService.Aspects;

public interface IOverdueTaskNotifier
{
    Task NotifyOverdueTasksAsync(CancellationToken cancellationToken);
}
