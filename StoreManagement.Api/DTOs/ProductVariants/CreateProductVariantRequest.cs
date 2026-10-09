using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.ProductVariants;

public class CreateProductVariantRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(100)]
    public string? VariantName { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }
}