using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Devices;

public class DeviceRegistrationSubmitRequest
{
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}