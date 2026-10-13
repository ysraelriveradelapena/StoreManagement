using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Devices;

public class DeviceTokenRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string DeviceSecret { get; set; } = string.Empty;
}