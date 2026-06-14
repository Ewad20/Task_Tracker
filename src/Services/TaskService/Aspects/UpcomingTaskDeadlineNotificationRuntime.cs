namespace TaskService.Aspects;

public static class UpcomingTaskDeadlineNotificationRuntime
{
    private static IServiceProvider? serviceProvider;

    public static void Configure(IServiceProvider provider)
    {
        serviceProvider = provider;
    }

    internal static async Task NotifyAsync()
    {
        if (serviceProvider is null)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IUpcomingTaskDeadlineNotifier>();
        await notifier.NotifyUpcomingDeadlinesAsync(CancellationToken.None);
    }
}
