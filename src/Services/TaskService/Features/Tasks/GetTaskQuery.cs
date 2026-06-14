using AutoMapper;
using MediatR;
using TaskService.Contracts.Tasks;
using TaskService.Repositories;

namespace TaskService.Features.Tasks;

public sealed record GetTaskQuery(Guid TaskId) : IRequest<TaskDto>;

public sealed class GetTaskHandler(ITaskRepository repository, IMapper mapper)
    : IRequestHandler<GetTaskQuery, TaskDto>
{
    public async Task<TaskDto> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(request.TaskId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found");

        return mapper.Map<TaskDto>(task);
    }
}
