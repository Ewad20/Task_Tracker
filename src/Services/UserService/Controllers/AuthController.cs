using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Contracts.Auth;
using UserService.Features.Auth;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [LogExecution("Authentication API")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterUserCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [LogExecution("Authentication API")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginUserCommand(request), cancellationToken);
        return Ok(result);
    }
}
