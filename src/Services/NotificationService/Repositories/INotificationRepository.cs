using NotificationService.Entities;

namespace NotificationService.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(
        string userId,
        Guid? projectId,
        CancellationToken cancellationToken);
    Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
}
