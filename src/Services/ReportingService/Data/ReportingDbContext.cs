using Microsoft.EntityFrameworkCore;
using ReportingService.Entities;

namespace ReportingService.Data;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<ProjectReport> ProjectReports => Set<ProjectReport>();
}
