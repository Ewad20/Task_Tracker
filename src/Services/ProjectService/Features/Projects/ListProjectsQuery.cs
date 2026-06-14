using AutoMapper;
using MediatR;
using ProjectService.Contracts.Projects;
using ProjectService.Repositories;

namespace ProjectService.Features.Projects;

public sealed record ListProjectsQuery(string UserId, bool IsAdmin) : IRequest<IReadOnlyList<ProjectDto>>;

public sealed class ListProjectsHandler(IProjectRepository repository, IMapper mapper)
    : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await repository.GetAllAsync(request.UserId, request.IsAdmin, cancellationToken);
        return projects.Select(mapper.Map<ProjectDto>).ToList();
    }
}
