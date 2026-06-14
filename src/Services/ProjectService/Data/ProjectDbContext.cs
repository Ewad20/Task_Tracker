using Microsoft.EntityFrameworkCore;
using ProjectService.Entities;

namespace ProjectService.Data;

public sealed class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
}
