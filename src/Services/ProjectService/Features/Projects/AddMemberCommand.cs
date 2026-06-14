using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectService.Contracts.Projects;
using ProjectService.Data;
using ProjectService.Entities;
using ProjectService.Messaging;

namespace ProjectService.Features.Projects;

public sealed record AddMemberCommand(Guid ProjectId, AddMemberRequest Request) : IRequest;

public sealed class AddMemberHandler(
    ProjectDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<AddMemberHandler> logger)
    : IRequestHandler<AddMemberCommand>
{
    public async Task<Unit> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(project => project.Members)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found");

        if (project.Members.Any(member => member.UserId == request.Request.UserId))
        {
            return Unit.Value;
        }

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = request.Request.UserId,
            Role = request.Request.Role
        };

        dbContext.ProjectMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            eventPublisher.Publish("projects.member-added", new { project.Id, member.UserId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Project member was added, but publishing projects.member-added event failed.");
        }

        return Unit.Value;
    }
}
