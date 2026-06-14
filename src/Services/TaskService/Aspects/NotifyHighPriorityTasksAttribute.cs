using BuildingBlocks.Aspects;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Serialization;

namespace TaskService.Aspects;

[PSerializable]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(LogExecutionAttribute))]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(NotifyOverdueTasksAttribute))]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(NotifyUpcomingTaskDeadlinesAttribute))]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class NotifyHighPriorityTasksAttribute : MethodInterceptionAspect
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
            await HighPriorityTaskNotificationRuntime.NotifyAsync();
        }
        catch
        {
            // Business notifications must not roll back the user operation.
        }
    }
}
