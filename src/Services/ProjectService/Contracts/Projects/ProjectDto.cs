namespace ProjectService.Contracts.Projects;

public sealed record ProjectMemberDto(Guid Id, string UserId, string Role);

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    string OwnerId,
    DateTime CreatedAt,
    IReadOnlyCollection<ProjectMemberDto> Members);
