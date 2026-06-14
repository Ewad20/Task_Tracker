using Microsoft.EntityFrameworkCore;
using AuditService.Entities;

namespace AuditService.Data;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
