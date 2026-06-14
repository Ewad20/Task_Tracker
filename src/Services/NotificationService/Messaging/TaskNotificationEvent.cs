namespace NotificationService.Messaging;

public sealed record TaskNotificationEvent(
    string UserId,
    string Message,
    Guid TaskId,
    Guid ProjectId,
    string EventType);
