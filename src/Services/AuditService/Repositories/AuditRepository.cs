using Microsoft.EntityFrameworkCore;
using AuditService.Data;
using AuditService.Entities;

namespace AuditService.Repositories;

public sealed class AuditRepository(AuditDbContext dbContext) : IAuditRepository
{
    public async Task<IReadOnlyList<AuditLog>> GetFilteredAsync(AuditFilter filter, CancellationToken cancellationToken)
    {
        IQueryable<AuditLog> query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(log => log.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(log => log.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(log => log.EntityType == filter.EntityType);
        }

        return await query.OrderByDescending(log => log.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
