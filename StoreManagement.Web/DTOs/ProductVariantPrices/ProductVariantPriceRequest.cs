
namespace StoreManagement.Web.DTOs.ProductVariantPrices;

public class ProductVariantPriceRequest
{
    public int ProductVariantId { get; set; }

    public decimal Price { get; set; }

    public string PriceType { get; set; } = "REGULAR";

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
