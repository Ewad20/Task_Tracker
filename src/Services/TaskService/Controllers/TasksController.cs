using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskService.Aspects;
using TaskService.Contracts.Tasks;
using TaskService.Features.Tasks;

namespace TaskService.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [LogExecution("Tasks API")]
    [NotifyOverdueTasks]
    [NotifyUpcomingTaskDeadlines]
    [NotifyHighPriorityTasks]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll([FromQuery] TaskFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTasksQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{taskId:guid}")]
    [LogExecution("Tasks API")]
    [NotifyOverdueTasks]
    [NotifyUpcomingTaskDeadlines]
    [NotifyHighPriorityTasks]
    public async Task<ActionResult<TaskDto>> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(taskId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [LogExecution("Tasks API")]
    [NotifyOverdueTasks]
    [NotifyUpcomingTaskDeadlines]
    [NotifyHighPriorityTasks]
    [RefreshProjectReport]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTaskCommand(request, GetCurrentUserId(), User.IsInRole("Admin")),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { taskId = result.Id }, result);
    }

    [HttpPut("{taskId:guid}")]
    [LogExecution("Tasks API")]
    [NotifyOverdueTasks]
    [NotifyUpcomingTaskDeadlines]
    [NotifyHighPriorityTasks]
    [RefreshProjectReport]
    public async Task<ActionResult<TaskDto>> Update(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateTaskCommand(taskId, request, GetCurrentUserId(), User.IsInRole("Admin")),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{taskId:guid}")]
    [LogExecution("Tasks API")]
    [NotifyOverdueTasks]
    [NotifyUpcomingTaskDeadlines]
    [NotifyHighPriorityTasks]
    [RefreshProjectReport]
    public async Task<IActionResult> Delete(Guid taskId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTaskCommand(taskId, GetCurrentUserId(), User.IsInRole("Admin")), cancellationToken);
        return NoContent();
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
