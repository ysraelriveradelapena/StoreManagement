
namespace StoreManagement.Web.DTOs.Devices;

public class ApproveDeviceRegistrationResponseDto
{
    public int RequestId { get; set; }
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceSecret { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
