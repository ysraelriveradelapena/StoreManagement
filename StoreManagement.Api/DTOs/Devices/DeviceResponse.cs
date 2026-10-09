namespace StoreManagement.Api.DTOs.Devices;

public class DeviceResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public string? StoreName { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastSyncAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}