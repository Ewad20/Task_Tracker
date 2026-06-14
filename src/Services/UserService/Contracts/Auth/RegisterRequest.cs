using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(120, MinimumLength = 2)] string DisplayName);
