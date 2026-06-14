using System.Diagnostics;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace BuildingBlocks.Aspects;

[PSerializable]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class LogExecutionAttribute : MethodInterceptionAspect
{
    public LogExecutionAttribute(string? operationName = null)
    {
        OperationName = operationName;
    }

    public string? OperationName { get; set; }

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        if (ShouldSkip(args.Method))
        {
            args.Proceed();
            return;
        }

        var operationName = ResolveOperationName(args.Method);
        var stopwatch = Stopwatch.StartNew();

        AspectExecutionLogger.LogEntering(operationName);

        try
        {
            args.Proceed();
            stopwatch.Stop();
            AspectExecutionLogger.LogExited(operationName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AspectExecutionLogger.LogException(ex, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        if (ShouldSkip(args.Method))
        {
            await args.ProceedAsync();
            return;
        }

        var operationName = ResolveOperationName(args.Method);
        var stopwatch = Stopwatch.StartNew();

        AspectExecutionLogger.LogEntering(operationName);

        try
        {
            await args.ProceedAsync();
            stopwatch.Stop();
            AspectExecutionLogger.LogExited(operationName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AspectExecutionLogger.LogException(ex, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private string ResolveOperationName(MethodBase method)
    {
        return OperationName
            ?? $"{method.DeclaringType?.Name ?? "UnknownType"}.{method.Name}";
    }

    private static bool ShouldSkip(MethodBase method)
    {
        return method.IsDefined(typeof(SkipExecutionLoggingAttribute), true)
            || method.DeclaringType?.IsDefined(typeof(SkipExecutionLoggingAttribute), true) == true;
    }
}
