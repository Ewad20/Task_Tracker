using System.Security.Claims;
using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.Contracts.Projects;
using ProjectService.Features.Projects;

namespace ProjectService.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    [LogExecution("Projects API")]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListProjectsQuery(CurrentUserId, IsAdmin), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(projectId, CurrentUserId, IsAdmin), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProjectCommand(CurrentUserId, request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId = result.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<ActionResult<ProjectDto>> Update(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateProjectCommand(projectId, request), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{projectId:guid}")]
    [LogExecution("Projects API")]
    public async Task<IActionResult> Delete(Guid projectId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectCommand(projectId), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{projectId:guid}/members")]
    [LogExecution("Projects API")]
    public async Task<IActionResult> AddMember(Guid projectId, AddMemberRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new AddMemberCommand(projectId, request), cancellationToken);
        return NoContent();
    }
}
