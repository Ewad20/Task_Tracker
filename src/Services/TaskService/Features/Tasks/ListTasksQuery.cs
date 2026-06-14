using AutoMapper;
using MediatR;
using TaskService.Contracts.Tasks;
using TaskService.Repositories;

namespace TaskService.Features.Tasks;

public sealed record ListTasksQuery(TaskFilterRequest Filter) : IRequest<IReadOnlyList<TaskDto>>;

public sealed class ListTasksHandler(ITaskRepository repository, IMapper mapper)
    : IRequestHandler<ListTasksQuery, IReadOnlyList<TaskDto>>
{
    public async Task<IReadOnlyList<TaskDto>> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        var filter = new TaskFilter
        {
            ProjectId = request.Filter.ProjectId,
            AssigneeId = request.Filter.AssigneeId,
            Status = request.Filter.Status,
            Priority = request.Filter.Priority,
            Search = request.Filter.Search
        };

        var tasks = await repository.GetFilteredAsync(filter, cancellationToken);
        return tasks.Select(mapper.Map<TaskDto>).ToList();
    }
}
