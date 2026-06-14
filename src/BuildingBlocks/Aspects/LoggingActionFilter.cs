using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Aspects;

public sealed class LoggingActionFilter(ILogger<LoggingActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ShouldSkip(context))
        {
            await next();
            return;
        }

        var operationName = ResolveOperationName(context);
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Entering {OperationName}", operationName);

        var executedContext = await next();

        stopwatch.Stop();

        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            logger.LogError(
                executedContext.Exception,
                "Exception in {OperationName} after {ElapsedMilliseconds} ms",
                operationName,
                stopwatch.ElapsedMilliseconds);
            return;
        }

        logger.LogInformation(
            "Exited {OperationName} in {ElapsedMilliseconds} ms",
            operationName,
            stopwatch.ElapsedMilliseconds);
    }

    private static bool ShouldSkip(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return false;
        }

        return descriptor.ControllerTypeInfo.IsDefined(typeof(SkipExecutionLoggingAttribute), true)
            || descriptor.MethodInfo.IsDefined(typeof(SkipExecutionLoggingAttribute), true);
    }

    private static string ResolveOperationName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return context.ActionDescriptor.DisplayName ?? "HTTP action";
        }

        var methodAttribute = descriptor.MethodInfo
            .GetCustomAttributes(typeof(LogExecutionAttribute), true)
            .OfType<LogExecutionAttribute>()
            .FirstOrDefault();

        var controllerAttribute = descriptor.ControllerTypeInfo
            .GetCustomAttributes(typeof(LogExecutionAttribute), true)
            .OfType<LogExecutionAttribute>()
            .FirstOrDefault();

        return methodAttribute?.OperationName
            ?? controllerAttribute?.OperationName
            ?? $"{descriptor.ControllerName}.{descriptor.ActionName}";
    }
}
