using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Users;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);
