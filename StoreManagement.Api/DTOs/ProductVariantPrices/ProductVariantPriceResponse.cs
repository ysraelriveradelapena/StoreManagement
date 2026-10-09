namespace StoreManagement.Api.DTOs.ProductVariantPrices;

public class ProductVariantPriceResponse
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? VariantName { get; set; }

    public decimal Price { get; set; }

    public string PriceType { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }
}