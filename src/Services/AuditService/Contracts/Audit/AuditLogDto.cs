namespace AuditService.Contracts.Audit;

public sealed record AuditLogDto(Guid Id, string UserId, string Action, string EntityType, string Payload, DateTime CreatedAt);
