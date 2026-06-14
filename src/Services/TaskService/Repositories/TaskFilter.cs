using TaskPriority = TaskService.Entities.TaskPriority;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Repositories;

public sealed class TaskFilter
{
    public Guid? ProjectId { get; set; }
    public string? AssigneeId { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? Search { get; set; }
}
