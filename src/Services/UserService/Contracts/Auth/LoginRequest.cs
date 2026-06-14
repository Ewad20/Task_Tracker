using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
