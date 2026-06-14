using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Contracts.Reports;
using ReportingService.Features.Reports;

namespace ReportingService.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [LogExecution("Reports API")]
    public async Task<ActionResult<IReadOnlyList<ProjectReportDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListReportsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    [LogExecution("Reports API")]
    public async Task<ActionResult<ProjectReportDto>> GetByProjectId(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectReportQuery(projectId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [LogExecution("Reports API")]
    public async Task<ActionResult<ProjectReportDto>> Upsert(UpsertProjectReportRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpsertProjectReportCommand(request), cancellationToken);
        return Ok(result);
    }
}
