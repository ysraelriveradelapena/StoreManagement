using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Devices;

public class RejectDeviceRegistrationRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}