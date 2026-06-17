using BuildingBlocks.Aspects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Contracts.Auth;
using UserService.Features.Auth;

namespace UserService.Controllers;

/// <summary>
/// Zarządza uwierzytelnianiem użytkowników — rejestracja i logowanie.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Rejestruje nowego użytkownika w systemie i zwraca token JWT.
    /// </summary>
    /// <param name="request">Dane rejestracyjne: email, hasło, nazwa wyświetlana.</param>
    /// <param name="cancellationToken">Token anulowania żądania.</param>
    /// <returns>Token JWT oraz czas wygaśnięcia sesji.</returns>
    /// <response code="200">Rejestracja powiodła się.</response>
    /// <response code="400">Dane rejestracyjne są nieprawidłowe.</response>
    [HttpPost("register")]
    [LogExecution("Authentication API")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterUserCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Loguje istniejącego użytkownika i zwraca token JWT.
    /// </summary>
    /// <param name="request">Dane logowania: email i hasło.</param>
    /// <param name="cancellationToken">Token anulowania żądania.</param>
    /// <returns>Token JWT oraz czas wygaśnięcia sesji.</returns>
    /// <response code="200">Logowanie powiodło się.</response>
    /// <response code="401">Nieprawidłowe dane logowania.</response>
    [HttpPost("login")]
    [LogExecution("Authentication API")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginUserCommand(request), cancellationToken);
        return Ok(result);
    }
}
