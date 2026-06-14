using System.ComponentModel.DataAnnotations;

namespace ProjectService.Contracts.Projects;

public sealed record CreateProjectRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [StringLength(1000)] string Description,
    IReadOnlyCollection<string>? MemberUserIds = null);
