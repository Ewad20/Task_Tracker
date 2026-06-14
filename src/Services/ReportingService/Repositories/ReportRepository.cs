using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Entities;

namespace ReportingService.Repositories;

public sealed class ReportRepository(ReportingDbContext dbContext) : IReportRepository
{
    public async Task<ProjectReport?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectReports.FirstOrDefaultAsync(r => r.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectReport>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ProjectReports.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProjectReport report, CancellationToken cancellationToken)
    {
        dbContext.ProjectReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProjectReport report, CancellationToken cancellationToken)
    {
        dbContext.ProjectReports.Update(report);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
