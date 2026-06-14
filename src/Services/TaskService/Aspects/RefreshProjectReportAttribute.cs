using BuildingBlocks.Aspects;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Serialization;
using TaskService.Contracts.Tasks;

namespace TaskService.Aspects;

[PSerializable]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(LogExecutionAttribute))]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(NotifyOverdueTasksAttribute))]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(NotifyUpcomingTaskDeadlinesAttribute))]
[AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(NotifyHighPriorityTasksAttribute))]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RefreshProjectReportAttribute : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        var projectId = ResolveProjectIdAsync(args).GetAwaiter().GetResult();
        args.Proceed();
        TryPublishReportUpdateAsync(projectId).GetAwaiter().GetResult();
    }

    public override async Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        var projectId = await ResolveProjectIdAsync(args);
        await args.ProceedAsync();
        await TryPublishReportUpdateAsync(projectId);
    }

    private static async Task<Guid?> ResolveProjectIdAsync(MethodInterceptionArgs args)
    {
        var createRequest = args.Arguments.OfType<CreateTaskRequest>().FirstOrDefault();
        if (createRequest is not null)
        {
            return createRequest.ProjectId;
        }

        var taskId = args.Arguments.OfType<Guid>().FirstOrDefault();
        if (taskId == Guid.Empty)
        {
            return null;
        }

        try
        {
            return await ProjectReportRefreshRuntime.ResolveProjectIdAsync(taskId);
        }
        catch
        {
            return null;
        }
    }

    private static async Task TryPublishReportUpdateAsync(Guid? projectId)
    {
        if (!projectId.HasValue)
        {
            return;
        }

        try
        {
            await ProjectReportRefreshRuntime.PublishReportUpdateAsync(projectId.Value);
        }
        catch
        {
            // Report refresh must not roll back the user operation.
        }
    }
}
