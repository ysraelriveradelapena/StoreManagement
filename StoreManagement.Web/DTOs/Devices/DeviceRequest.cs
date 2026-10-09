using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Devices;

public class DeviceRequest
{
    [Required(ErrorMessage = "Device code is required.")]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Device name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a store.")]
    public int StoreId { get; set; }
}