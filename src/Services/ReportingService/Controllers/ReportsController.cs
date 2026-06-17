using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Contracts.Reports;
using ReportingService.Features.Reports;

namespace ReportingService.Controllers;

/// <summary>
/// Udostępnia statystyki i raporty postępu prac dla projektów.
/// </summary>
[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Zwraca raporty postępu dla wszystkich projektów.
    /// </summary>
    [HttpGet]
    [LogExecution("Reports API")]
    public async Task<ActionResult<IReadOnlyList<ProjectReportDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListReportsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Zwraca raport postępu dla wskazanego projektu.
    /// </summary>
    [HttpGet("{projectId:guid}")]
    [LogExecution("Reports API")]
    public async Task<ActionResult<ProjectReportDto>> GetByProjectId(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectReportQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tworzy lub aktualizuje raport projektu.
    /// </summary>
    [HttpPost]
    [LogExecution("Reports API")]
    public async Task<ActionResult<ProjectReportDto>> Upsert(UpsertProjectReportRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpsertProjectReportCommand(request), cancellationToken);
        return Ok(result);
    }
}
