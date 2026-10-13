namespace StoreManagement.Api.DTOs.Devices;

public class DeviceCredentialResponse
{
    public int DeviceId { get; set; }

    public string DeviceCode { get; set; } = string.Empty;

    public string DeviceSecret { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}