namespace StoreManagement.Api.DTOs.ProductVariantPrices;

public class UpdateProductVariantPriceRequest
{
    public decimal Price { get; set; }

    public string PriceType { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}