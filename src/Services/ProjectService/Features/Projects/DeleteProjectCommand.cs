using MediatR;
using ProjectService.Messaging;
using ProjectService.Repositories;

namespace ProjectService.Features.Projects;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest;

public sealed class DeleteProjectHandler(
    IProjectRepository repository,
    IEventPublisher eventPublisher,
    ILogger<DeleteProjectHandler> logger)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetAsync(request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found");

        await repository.DeleteAsync(project, cancellationToken);
        try
        {
            eventPublisher.Publish("projects.deleted", new { project.Id });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Project was deleted, but publishing projects.deleted event failed.");
        }

        return Unit.Value;
    }
}
