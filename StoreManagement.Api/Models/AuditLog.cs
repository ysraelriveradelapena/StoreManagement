namespace StoreManagement.Api.Models;

public class AuditLog
{
    public long Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }
}