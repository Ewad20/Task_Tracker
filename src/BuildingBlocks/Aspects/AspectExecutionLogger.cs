using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Aspects;

public static class AspectExecutionLogger
{
    private const string LoggerCategory = "PostSharp.ExecutionLogging";
    private static ILoggerFactory? loggerFactory;

    public static void Configure(IServiceProvider serviceProvider)
    {
        loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    internal static void LogEntering(string operationName)
    {
        var logger = loggerFactory?.CreateLogger(LoggerCategory);
        if (logger is null)
        {
            Console.WriteLine($"Entering {operationName}");
            return;
        }

        logger.LogInformation("Entering {OperationName}", operationName);
    }

    internal static void LogExited(string operationName, long elapsedMilliseconds)
    {
        var logger = loggerFactory?.CreateLogger(LoggerCategory);
        if (logger is null)
        {
            Console.WriteLine($"Exited {operationName} in {elapsedMilliseconds} ms");
            return;
        }

        logger.LogInformation(
            "Exited {OperationName} in {ElapsedMilliseconds} ms",
            operationName,
            elapsedMilliseconds);
    }

    internal static void LogException(Exception exception, string operationName, long elapsedMilliseconds)
    {
        var logger = loggerFactory?.CreateLogger(LoggerCategory);
        if (logger is null)
        {
            Console.WriteLine($"Exception in {operationName} after {elapsedMilliseconds} ms: {exception}");
            return;
        }

        logger.LogError(
            exception,
            "Exception in {OperationName} after {ElapsedMilliseconds} ms",
            operationName,
            elapsedMilliseconds);
    }
}
