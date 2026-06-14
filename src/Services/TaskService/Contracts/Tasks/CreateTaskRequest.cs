using System.ComponentModel.DataAnnotations;
using TaskService.Entities;

namespace TaskService.Contracts.Tasks;

public sealed record CreateTaskRequest(
    [Required] Guid ProjectId,
    [Required, StringLength(160, MinimumLength = 2)] string Title,
    [StringLength(2000)] string Description,
    [Required] string AssigneeId,
    TaskPriority Priority,
    DateTime? DueDate);
