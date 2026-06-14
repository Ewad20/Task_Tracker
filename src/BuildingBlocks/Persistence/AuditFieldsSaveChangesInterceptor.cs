using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence;

public sealed class AuditFieldsSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampAuditFields(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                TrySetDateTime(entry, "CreatedAt", now, onlyWhenDefault: true);
                TrySetDateTime(entry, "UpdatedAt", now, onlyWhenDefault: true);
            }
            else if (entry.State == EntityState.Modified)
            {
                TrySetDateTime(entry, "UpdatedAt", now, onlyWhenDefault: false);
            }
        }
    }

    private static void TrySetDateTime(EntityEntry entry, string propertyName, DateTime value, bool onlyWhenDefault)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        if (property is null || property.ClrType != typeof(DateTime))
        {
            return;
        }

        var currentValue = entry.Property(propertyName).CurrentValue;
        if (onlyWhenDefault && currentValue is DateTime dateTime && dateTime != default)
        {
            return;
        }

        entry.Property(propertyName).CurrentValue = value;
    }
}
