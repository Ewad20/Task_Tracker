namespace AuditService.Repositories;

public sealed class AuditFilter
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
}
