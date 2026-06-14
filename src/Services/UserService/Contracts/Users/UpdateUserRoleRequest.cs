using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Users;

public sealed record UpdateUserRoleRequest([Required, StringLength(50)] string Role);
