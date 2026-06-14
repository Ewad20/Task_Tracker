namespace TaskService.Aspects;

public static class ProjectReportRefreshRuntime
{
    private static IServiceProvider? serviceProvider;

    public static void Configure(IServiceProvider provider)
    {
        serviceProvider = provider;
    }

    internal static async Task<Guid?> ResolveProjectIdAsync(Guid taskId)
    {
        if (serviceProvider is null)
        {
            return null;
        }

        using var scope = serviceProvider.CreateScope();
        var refresher = scope.ServiceProvider.GetRequiredService<IProjectReportRefresher>();
        return await refresher.ResolveProjectIdAsync(taskId, CancellationToken.None);
    }

    internal static async Task PublishReportUpdateAsync(Guid projectId)
    {
        if (serviceProvider is null)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var refresher = scope.ServiceProvider.GetRequiredService<IProjectReportRefresher>();
        await refresher.PublishReportUpdateAsync(projectId, CancellationToken.None);
    }
}
