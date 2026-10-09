using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Devices;

public class CreateDeviceRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }
}