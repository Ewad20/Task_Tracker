using System.Security.Claims;
using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.Contracts.Projects;
using ProjectService.Features.Projects;

namespace ProjectService.Controllers;

/// <summary>
/// Zarządza projektami zespołowymi.
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// Zwraca listę projektów dostępnych dla aktualnego użytkownika.
    /// </summary>
    [HttpGet]
    [LogExecution("Projects API")]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListProjectsQuery(CurrentUserId, IsAdmin), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Zwraca szczegóły wskazanego projektu.
    /// </summary>
    [HttpGet("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(projectId, CurrentUserId, IsAdmin), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tworzy nowy projekt (tylko Admin).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProjectCommand(CurrentUserId, request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId = result.Id }, result);
    }

    /// <summary>
    /// Aktualizuje dane projektu (tylko Admin).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> Update(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateProjectCommand(projectId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Usuwa projekt (tylko Admin).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<IActionResult> Delete(Guid projectId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectCommand(projectId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Dodaje nowego członka do projektu (tylko Admin).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{projectId:guid}/members")]
    [LogExecution("Projects API")]
    public async Task<IActionResult> AddMember(Guid projectId, AddMemberRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new AddMemberCommand(projectId, request), cancellationToken);
        return NoContent();
    }
}
