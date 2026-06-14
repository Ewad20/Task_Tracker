namespace TaskService.Aspects;

public interface IProjectReportRefresher
{
    Task<Guid?> ResolveProjectIdAsync(Guid taskId, CancellationToken cancellationToken);
    Task PublishReportUpdateAsync(Guid projectId, CancellationToken cancellationToken);
}
