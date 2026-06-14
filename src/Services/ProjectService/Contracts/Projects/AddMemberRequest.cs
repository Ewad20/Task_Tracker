using System.ComponentModel.DataAnnotations;

namespace ProjectService.Contracts.Projects;

public sealed record AddMemberRequest(
    [Required] string UserId,
    [Required, StringLength(50)] string Role);
