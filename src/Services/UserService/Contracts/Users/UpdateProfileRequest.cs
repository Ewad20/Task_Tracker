using System.ComponentModel.DataAnnotations;

namespace UserService.Contracts.Users;

public sealed record UpdateProfileRequest(
    [Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    [StringLength(1000)] string Bio);
