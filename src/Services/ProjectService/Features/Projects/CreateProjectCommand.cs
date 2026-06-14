using AutoMapper;
using MediatR;
using ProjectService.Contracts.Projects;
using ProjectService.Entities;
using ProjectService.Messaging;
using ProjectService.Repositories;

namespace ProjectService.Features.Projects;

public sealed record CreateProjectCommand(string OwnerId, CreateProjectRequest Request) : IRequest<ProjectDto>;

public sealed class CreateProjectHandler(
    IProjectRepository repository,
    IEventPublisher eventPublisher,
    ILogger<CreateProjectHandler> logger,
    IMapper mapper)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private static IReadOnlyList<string> GetDistinctMemberUserIds(CreateProjectRequest request) =>
        (request.MemberUserIds ?? Array.Empty<string>())
            .Select(userId => userId.Trim())
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Request.Name,
            Description = request.Request.Description,
            OwnerId = request.OwnerId
        };

        foreach (var userId in GetDistinctMemberUserIds(request.Request))
        {
            project.Members.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = userId,
                Role = "Member"
            });
        }

        await repository.AddAsync(project, cancellationToken);
        try
        {
            eventPublisher.Publish("projects.created", new { project.Id, project.Name, project.OwnerId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Project was created, but publishing projects.created event failed.");
        }

        return mapper.Map<ProjectDto>(project);
    }
}
