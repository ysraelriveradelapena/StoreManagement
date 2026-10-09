namespace StoreManagement.Api.Models;

public class ProductVariantPrice
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public decimal Price { get; set; }

    public string PriceType { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;
}