using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuditService.Contracts.Audit;
using AuditService.Features.Audit;

namespace AuditService.Controllers;

[ApiController]
[Authorize]
[Route("api/audit")]
public sealed class AuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [LogExecution("Audit API")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> Get([FromQuery] AuditFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(filter), cancellationToken);
        return Ok(result);
    }
}
