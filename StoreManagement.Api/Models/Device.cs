namespace StoreManagement.Api.Models;

public class Device
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastSyncAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Store? Store { get; set; }
}