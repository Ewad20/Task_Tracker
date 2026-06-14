using Microsoft.AspNetCore.SignalR;
using NotificationService.Entities;
using NotificationService.Hubs;
using NotificationService.Repositories;

namespace NotificationService.Messaging;

public sealed class TaskNotificationProcessor(
    TaskNotificationStream stream,
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub> hubContext,
    ILogger<TaskNotificationProcessor> logger) : BackgroundService
{
    private IDisposable? subscription;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        subscription = stream.Subscribe(new Observer(notification =>
            _ = PersistAndBroadcastAsync(notification, stoppingToken)));

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        subscription?.Dispose();
        stream.Complete();
        return base.StopAsync(cancellationToken);
    }

    private async Task PersistAndBroadcastAsync(
        TaskNotificationEvent notificationEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = notificationEvent.UserId,
                Message = notificationEvent.Message,
                ProjectId = notificationEvent.ProjectId,
                TaskId = notificationEvent.TaskId,
                EventType = notificationEvent.EventType,
                IsRead = false
            };

            await repository.AddAsync(notification, cancellationToken);

            var payload = new
            {
                notification.Id,
                notification.UserId,
                notification.Message,
                notification.ProjectId,
                notification.TaskId,
                notification.EventType,
                notification.IsRead,
                notification.CreatedAt,
            };

            await hubContext.Clients.Group(notification.UserId)
                .SendAsync("notification", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process task notification stream event.");
        }
    }

    private sealed class Observer(Action<TaskNotificationEvent> onNext) : IObserver<TaskNotificationEvent>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(TaskNotificationEvent value)
        {
            onNext(value);
        }
    }
}
