namespace TaskService.Aspects;

public interface IUpcomingTaskDeadlineNotifier
{
    Task NotifyUpcomingDeadlinesAsync(CancellationToken cancellationToken);
}
