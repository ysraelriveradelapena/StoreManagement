
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.ProductVariants;

public class ProductVariantRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Barcode { get; set; }

    [StringLength(100)]
    public string? VariantName { get; set; }

    [StringLength(50)]
    public string? Size { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }
}
