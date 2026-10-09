using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Stores;

public class UpdateStoreRequest
{
    [Required]
    [MaxLength(50)]
    public string StoreCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DepartmentStore { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }
}