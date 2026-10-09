namespace StoreManagement.Api.Models.Identity;

public class RefreshToken
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsActive => !IsRevoked && !IsExpired;

    public ApplicationUser? User { get; set; }
}