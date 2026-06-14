using Microsoft.EntityFrameworkCore;
using ProjectService.Data;
using ProjectService.Entities;

namespace ProjectService.Repositories;

public sealed class ProjectRepository(ProjectDbContext dbContext) : IProjectRepository
{
    public async Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> GetAccessibleAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.Include(p => p.Members).AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(project =>
                project.OwnerId == userId ||
                project.Members.Any(member => member.UserId == userId));
        }

        return await query.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(string userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Members)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(project =>
                project.OwnerId == userId ||
                project.Members.Any(member => member.UserId == userId));
        }

        return await query.OrderBy(project => project.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
