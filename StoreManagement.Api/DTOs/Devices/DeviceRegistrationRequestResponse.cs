namespace StoreManagement.Api.DTOs.Devices;

public class DeviceRegistrationRequestResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; }

    public string RequestedByUserId { get; set; } = string.Empty;
}