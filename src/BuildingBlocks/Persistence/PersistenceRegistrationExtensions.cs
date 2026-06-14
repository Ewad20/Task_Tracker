using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Persistence;

public static class PersistenceRegistrationExtensions
{
    public static IServiceCollection AddSqlServerDbContextWithAudit<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        services.TryAddScoped<AuditFieldsSaveChangesInterceptor>();

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditFieldsSaveChangesInterceptor>());
        });

        return services;
    }
}
