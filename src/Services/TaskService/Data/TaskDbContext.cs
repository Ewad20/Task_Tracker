using Microsoft.EntityFrameworkCore;
using TaskService.Entities;

namespace TaskService.Data;

public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
