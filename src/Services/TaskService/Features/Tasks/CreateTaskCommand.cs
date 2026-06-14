using AutoMapper;
using MediatR;
using TaskService.Contracts.Tasks;
using TaskService.Entities;
using TaskService.Messaging;
using TaskService.Repositories;

namespace TaskService.Features.Tasks;

public sealed record CreateTaskCommand(CreateTaskRequest Request, string CurrentUserId, bool IsAdmin) : IRequest<TaskDto>;

public sealed class CreateTaskHandler(
    ITaskRepository repository,
    IEventPublisher eventPublisher,
    ILogger<CreateTaskHandler> logger,
    IMapper mapper)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var assigneeId = request.IsAdmin ? request.Request.AssigneeId : request.CurrentUserId;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.Request.ProjectId,
            Title = request.Request.Title,
            Description = request.Request.Description,
            AssigneeId = assigneeId,
            Priority = request.Request.Priority,
            DueDate = request.Request.DueDate
        };

        await repository.AddAsync(task, cancellationToken);
        try
        {
            eventPublisher.Publish("tasks.created", new { task.Id, task.ProjectId, task.Title });
            if (!string.IsNullOrWhiteSpace(task.AssigneeId))
            {
                eventPublisher.Publish(
                    "tasks.assigned",
                    new TaskAssignedEvent(
                        task.Id,
                        task.ProjectId,
                        task.Title,
                        task.AssigneeId,
                        PreviousAssigneeId: null));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Task was created, but publishing tasks.created event failed.");
        }

        return mapper.Map<TaskDto>(task);
    }
}
