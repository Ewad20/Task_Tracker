using System.ComponentModel.DataAnnotations;
using TaskPriority = TaskService.Entities.TaskPriority;
using TaskStatus = TaskService.Entities.TaskStatus;

namespace TaskService.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    [Required, StringLength(160, MinimumLength = 2)] string Title,
    [StringLength(2000)] string Description,
    [Required] string AssigneeId,
    TaskPriority Priority,
    TaskStatus Status,
    DateTime? DueDate);
