using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Stores;

public class StoreRequest
{
    [Required(ErrorMessage = "Store code is required.")]
    [MaxLength(50)]
    public string StoreCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Store name is required.")]
    [MaxLength(150)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DepartmentStore { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }
}