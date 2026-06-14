using BuildingBlocks.Validation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Aspects;

public static class AspectRegistrationExtensions
{
    public static IServiceCollection AddApplicationAspects(this IServiceCollection services)
    {
        services.TryAddScoped<NullRequestActionFilter>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataAnnotationsValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    public static IMvcBuilder AddControllersWithApplicationAspects(this IServiceCollection services)
    {
        services.AddApplicationAspects();

        return services.AddControllers(options =>
        {
            options.Filters.Add<NullRequestActionFilter>();
        });
    }
}
