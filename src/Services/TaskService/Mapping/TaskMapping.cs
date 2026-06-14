using AutoMapper;
using TaskService.Contracts.Tasks;
using TaskService.Entities;

namespace TaskService.Mapping;

public sealed class TaskMapping : Profile
{
    public TaskMapping()
    {
        CreateMap<TaskItem, TaskDto>();
    }
}
