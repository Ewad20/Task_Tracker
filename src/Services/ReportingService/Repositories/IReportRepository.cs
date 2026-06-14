using ReportingService.Entities;

namespace ReportingService.Repositories;

public interface IReportRepository
{
    Task<ProjectReport?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectReport>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(ProjectReport report, CancellationToken cancellationToken);
    Task UpdateAsync(ProjectReport report, CancellationToken cancellationToken);
}
