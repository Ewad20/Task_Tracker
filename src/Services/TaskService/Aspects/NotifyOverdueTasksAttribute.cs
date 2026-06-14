using BuildingBlocks.Aspects;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Serialization;

namespace TaskService.Aspects;

[PSerializable]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(LogExecutionAttribute))]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class NotifyOverdueTasksAttribute : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        args.Proceed();
        TryNotifyAsync().GetAwaiter().GetResult();
    }

    public override async Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        await args.ProceedAsync();
        await TryNotifyAsync();
    }

    private static async Task TryNotifyAsync()
    {
        try
        {
            await OverdueTaskNotificationRuntime.NotifyAsync();
        }
        catch
        {
            // Business notifications must not roll back the user operation.
        }
    }
}
