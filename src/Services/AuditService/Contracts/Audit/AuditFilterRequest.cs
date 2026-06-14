namespace AuditService.Contracts.Audit;

public sealed record AuditFilterRequest(string? UserId, string? Action, string? EntityType);
