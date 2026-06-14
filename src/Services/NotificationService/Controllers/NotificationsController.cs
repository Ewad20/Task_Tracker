using System.Security.Claims;
using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Contracts.Notifications;
using NotificationService.Features.Notifications;

namespace NotificationService.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [LogExecution("Notifications API")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMyNotifications(
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await mediator.Send(new GetNotificationsQuery(userId, projectId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{notificationId:guid}")]
    [LogExecution("Notifications API")]
    public async Task<ActionResult<NotificationDto>> MarkRead(Guid notificationId, MarkReadRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkNotificationReadCommand(notificationId, request.IsRead), cancellationToken);
        return Ok(result);
    }
}
