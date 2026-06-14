using System.Security.Claims;
using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Contracts.Users;
using UserService.Features.Users;

namespace UserService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [LogExecution("Users API")]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [LogExecution("Users API")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await mediator.Send(new GetProfileQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [LogExecution("Users API")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await mediator.Send(new UpdateProfileCommand(userId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPut("me/password")]
    [LogExecution("Users API")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await mediator.Send(new ChangePasswordCommand(userId, request), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/role")]
    [LogExecution("Users API")]
    public async Task<ActionResult<UserProfileDto>> UpdateRole(
        string userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateUserRoleCommand(userId, request), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/password")]
    [LogExecution("Users API")]
    public async Task<IActionResult> ResetPassword(
        string userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new ResetUserPasswordCommand(userId, request), cancellationToken);
        return NoContent();
    }
}
