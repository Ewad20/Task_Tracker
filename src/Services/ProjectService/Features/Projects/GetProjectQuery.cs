using AutoMapper;
using MediatR;
using ProjectService.Contracts.Projects;
using ProjectService.Repositories;

namespace ProjectService.Features.Projects;

public sealed record GetProjectQuery(Guid ProjectId, string UserId, bool IsAdmin) : IRequest<ProjectDto>;

public sealed class GetProjectHandler(IProjectRepository repository, IMapper mapper)
    : IRequestHandler<GetProjectQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await repository.GetAccessibleAsync(request.ProjectId, request.UserId, request.IsAdmin, cancellationToken)
            ?? throw new InvalidOperationException("Project not found");

        return mapper.Map<ProjectDto>(project);
    }
}
