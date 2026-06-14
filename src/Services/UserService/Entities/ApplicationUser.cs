using Microsoft.AspNetCore.Identity;

namespace UserService.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
