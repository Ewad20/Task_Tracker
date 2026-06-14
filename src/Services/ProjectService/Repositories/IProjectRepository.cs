using ProjectService.Entities;

namespace ProjectService.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Project?> GetAccessibleAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetAllAsync(string userId, bool isAdmin, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task UpdateAsync(Project project, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}
