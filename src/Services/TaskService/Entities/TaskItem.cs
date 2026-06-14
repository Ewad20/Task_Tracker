namespace TaskService.Entities;

public sealed class TaskItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime? DueDate { get; set; }
    public DateTime? DueSoonNotificationSentAt { get; set; }
    public DateTime? HighPriorityNotificationSentAt { get; set; }
    public DateTime? OverdueNotificationSentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
