using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskService.Aspects;
using TaskService.Contracts.Tasks;
using TaskService.Features.Tasks;

namespace TaskService.Controllers;

/// <summary>
/// Zarządza zadaniami w ramach projektów.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Zwraca listę zadań z opcjonalnym filtrowaniem.
    /// </summary>
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

    /// <summary>
    /// Zwraca szczegóły wskazanego zadania.
    /// </summary>
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

    /// <summary>
    /// Tworzy nowe zadanie w projekcie.
    /// </summary>
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

    /// <summary>
    /// Aktualizuje istniejące zadanie.
    /// </summary>
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

    /// <summary>
    /// Usuwa wskazane zadanie.
    /// </summary>
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
