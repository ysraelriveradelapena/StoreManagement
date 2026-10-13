using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.Models;

public class Device
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public bool IsActive { get; set; } = true;

    // SHA-256 hash of the device secret.
    // Never store the actual secret here.
    [MaxLength(64)]
    public string? CredentialHash { get; set; }

    public DateTime? CredentialCreatedAtUtc { get; set; }

    public DateTime? LastSyncAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Store? Store { get; set; }
}