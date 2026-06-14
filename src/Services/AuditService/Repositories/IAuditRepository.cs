using AuditService.Entities;

namespace AuditService.Repositories;

public interface IAuditRepository
{
    Task<IReadOnlyList<AuditLog>> GetFilteredAsync(AuditFilter filter, CancellationToken cancellationToken);
    Task AddAsync(AuditLog log, CancellationToken cancellationToken);
}
