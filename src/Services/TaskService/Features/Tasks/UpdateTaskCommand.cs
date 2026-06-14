using AutoMapper;
using MediatR;
using TaskService.Contracts.Tasks;
using TaskService.Messaging;
using TaskService.Repositories;

namespace TaskService.Features.Tasks;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    UpdateTaskRequest Request,
    string CurrentUserId,
    bool IsAdmin) : IRequest<TaskDto>;

public sealed class UpdateTaskHandler(
    ITaskRepository repository,
    IEventPublisher eventPublisher,
    ILogger<UpdateTaskHandler> logger,
    IMapper mapper)
    : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(request.TaskId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found");

        if (!request.IsAdmin && task.AssigneeId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You can edit only tasks assigned to you.");
        }

        var previousAssigneeId = task.AssigneeId;
        var previousPriority = task.Priority;
        var previousStatus = task.Status;
        var previousDueDate = task.DueDate;

        task.Title = request.Request.Title;
        task.Description = request.Request.Description;
        task.AssigneeId = request.IsAdmin ? request.Request.AssigneeId : request.CurrentUserId;
        task.Priority = request.Request.Priority;
        task.Status = request.Request.Status;
        task.DueDate = request.Request.DueDate;

        if (previousDueDate != task.DueDate
            || previousStatus != task.Status
            || previousPriority != task.Priority
            || !string.Equals(previousAssigneeId, task.AssigneeId, StringComparison.Ordinal))
        {
            task.DueSoonNotificationSentAt = null;
            task.HighPriorityNotificationSentAt = null;
            task.OverdueNotificationSentAt = null;
        }

        await repository.UpdateAsync(task, cancellationToken);
        try
        {
            eventPublisher.Publish("tasks.updated", new { task.Id, task.Status, task.AssigneeId });

            if (!string.Equals(previousAssigneeId, task.AssigneeId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(task.AssigneeId))
            {
                eventPublisher.Publish(
                    "tasks.assigned",
                    new TaskAssignedEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        string.IsNullOrWhiteSpace(previousAssigneeId) ? null : previousAssigneeId));
            }

            if (previousStatus != task.Status && !string.IsNullOrWhiteSpace(task.AssigneeId))
            {
                eventPublisher.Publish(
                    "tasks.statusChanged",
                    new TaskStatusChangedEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        previousStatus,
                        task.Status));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Task was updated, but publishing tasks.updated event failed.");
        }

        return mapper.Map<TaskDto>(task);
    }
}
