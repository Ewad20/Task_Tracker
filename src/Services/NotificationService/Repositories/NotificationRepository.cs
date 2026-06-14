using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Entities;

namespace NotificationService.Repositories;

public sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(
        string userId,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId);

        if (projectId.HasValue)
        {
            query = query.Where(n => n.ProjectId == projectId);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Update(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
