namespace StoreManagement.Api.DTOs.ProductVariants;

public class ProductVariantResponse
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? VariantName { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}