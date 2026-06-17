using System.Security.Claims;
using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Contracts.Users;
using UserService.Features.Users;

namespace UserService.Controllers;

/// <summary>
/// Zarządza profilami użytkowników oraz administracją kont.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Zwraca listę wszystkich profili użytkowników.
    /// </summary>
    [HttpGet]
    [LogExecution("Users API")]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Zwraca profil aktualnie zalogowanego użytkownika.
    /// </summary>
    [HttpGet("me")]
    [LogExecution("Users API")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await mediator.Send(new GetProfileQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Aktualizuje profil aktualnie zalogowanego użytkownika.
    /// </summary>
    [HttpPut("me")]
    [LogExecution("Users API")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await mediator.Send(new UpdateProfileCommand(userId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Zmienia hasło aktualnie zalogowanego użytkownika.
    /// </summary>
    [HttpPut("me/password")]
    [LogExecution("Users API")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await mediator.Send(new ChangePasswordCommand(userId, request), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Zmienia rolę wskazanego użytkownika (tylko Admin).
    /// </summary>
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

    /// <summary>
    /// Wymusza reset hasła wskazanego użytkownika (tylko Admin).
    /// </summary>
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
