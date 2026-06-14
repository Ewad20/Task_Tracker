using Microsoft.EntityFrameworkCore;
using TaskService.Data;
using TaskService.Entities;

namespace TaskService.Repositories;

public sealed class TaskRepository(TaskDbContext dbContext) : ITaskRepository
{
    public async Task<TaskItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetFilteredAsync(TaskFilter filter, CancellationToken cancellationToken)
    {
        IQueryable<TaskItem> query = dbContext.Tasks.AsNoTracking();

        if (filter.ProjectId.HasValue)
        {
            query = query.Where(task => task.ProjectId == filter.ProjectId);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssigneeId))
        {
            query = query.Where(task => task.AssigneeId == filter.AssigneeId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(task => task.Status == filter.Status);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(task => task.Priority == filter.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(task => task.Title.Contains(filter.Search) || task.Description.Contains(filter.Search));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
