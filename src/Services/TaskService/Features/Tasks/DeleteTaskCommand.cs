using MediatR;
using TaskService.Messaging;
using TaskService.Repositories;

namespace TaskService.Features.Tasks;

public sealed record DeleteTaskCommand(Guid TaskId, string CurrentUserId, bool IsAdmin) : IRequest;

public sealed class DeleteTaskHandler(
    ITaskRepository repository,
    IEventPublisher eventPublisher,
    ILogger<DeleteTaskHandler> logger)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(request.TaskId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found");

        if (!request.IsAdmin && task.AssigneeId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You can delete only tasks assigned to you.");
        }

        await repository.DeleteAsync(task, cancellationToken);
        try
        {
            eventPublisher.Publish("tasks.deleted", new { task.Id, task.ProjectId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Task was deleted, but publishing tasks.deleted event failed.");
        }

        return Unit.Value;
    }
}
