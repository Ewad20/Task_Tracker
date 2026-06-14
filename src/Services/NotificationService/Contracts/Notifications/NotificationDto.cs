namespace NotificationService.Contracts.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string UserId,
    string Message,
    Guid? ProjectId,
    Guid? TaskId,
    string EventType,
    bool IsRead,
    DateTime CreatedAt);
