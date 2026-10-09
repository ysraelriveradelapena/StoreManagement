using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Brands;

public class CreateBrandRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}