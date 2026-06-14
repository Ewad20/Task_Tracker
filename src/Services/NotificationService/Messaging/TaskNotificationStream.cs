using System.Collections.Concurrent;

namespace NotificationService.Messaging;

public sealed class TaskNotificationStream : IObservable<TaskNotificationEvent>
{
    private readonly ConcurrentDictionary<IObserver<TaskNotificationEvent>, byte> observers = new();

    public IDisposable Subscribe(IObserver<TaskNotificationEvent> observer)
    {
        observers.TryAdd(observer, 0);
        return new Subscription(observers, observer);
    }

    public void Publish(TaskNotificationEvent notification)
    {
        foreach (var observer in observers.Keys)
        {
            observer.OnNext(notification);
        }
    }

    public void Complete()
    {
        foreach (var observer in observers.Keys)
        {
            observer.OnCompleted();
        }
    }

    private sealed class Subscription(
        ConcurrentDictionary<IObserver<TaskNotificationEvent>, byte> observers,
        IObserver<TaskNotificationEvent> observer) : IDisposable
    {
        public void Dispose()
        {
            observers.TryRemove(observer, out _);
        }
    }
}
