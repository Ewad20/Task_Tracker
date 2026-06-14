using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Users;

public sealed record ResetUserPasswordRequest([Required, MinLength(8)] string NewPassword);
