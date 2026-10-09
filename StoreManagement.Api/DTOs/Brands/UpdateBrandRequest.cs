using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Brands;

public class UpdateBrandRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}