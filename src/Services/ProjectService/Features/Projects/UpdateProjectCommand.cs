using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectService.Contracts.Projects;
using ProjectService.Data;
using ProjectService.Entities;
using ProjectService.Messaging;

namespace ProjectService.Features.Projects;

public sealed record UpdateProjectCommand(Guid ProjectId, UpdateProjectRequest Request) : IRequest<ProjectDto>;

public sealed class UpdateProjectHandler(
    ProjectDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<UpdateProjectHandler> logger,
    IMapper mapper)
    : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private static IReadOnlyList<string> GetDistinctMemberUserIds(UpdateProjectRequest request) =>
        (request.MemberUserIds ?? Array.Empty<string>())
            .Select(userId => userId.Trim())
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var requestedMemberUserIds = GetDistinctMemberUserIds(request.Request);

        var affectedProjects = await dbContext.Projects
            .Where(project => project.Id == request.ProjectId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(project => project.Name, request.Request.Name)
                    .SetProperty(project => project.Description, request.Request.Description),
                cancellationToken);

        if (affectedProjects == 0)
        {
            throw new InvalidOperationException("Project not found");
        }

        await dbContext.ProjectMembers
            .Where(member => member.ProjectId == request.ProjectId)
            .ExecuteDeleteAsync(cancellationToken);

        var members = requestedMemberUserIds.Select(userId => new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            UserId = userId,
            Role = "Member"
        });

        dbContext.ProjectMembers.AddRange(members);
        await dbContext.SaveChangesAsync(cancellationToken);

        var project = await dbContext.Projects
            .AsNoTracking()
            .Include(item => item.Members)
            .FirstAsync(item => item.Id == request.ProjectId, cancellationToken);

        try
        {
            eventPublisher.Publish("projects.updated", new { project.Id, project.Name });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Project was updated, but publishing projects.updated event failed.");
        }

        return mapper.Map<ProjectDto>(project);
    }
}
