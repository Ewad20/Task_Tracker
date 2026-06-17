using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuditService.Contracts.Audit;
using AuditService.Features.Audit;

namespace AuditService.Controllers;

/// <summary>
/// Udostępnia historię zdarzeń systemu do celów audytu.
/// </summary>
[ApiController]
[Authorize]
[Route("api/audit")]
public sealed class AuditController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Zwraca logi audytu z opcjonalnym filtrowaniem po użytkowniku, akcji i typie encji.
    /// </summary>
    [HttpGet]
    [LogExecution("Audit API")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> Get([FromQuery] AuditFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(filter), cancellationToken);
        return Ok(result);
    }
}
