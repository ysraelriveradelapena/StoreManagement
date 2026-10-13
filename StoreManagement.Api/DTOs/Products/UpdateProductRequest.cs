
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Products;

public class UpdateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? BrandId { get; set; }

    public int? CategoryId { get; set; }

    public bool NonExchangeable { get; set; } = false;
}
