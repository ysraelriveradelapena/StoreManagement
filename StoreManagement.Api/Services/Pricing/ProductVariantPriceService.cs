using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.ProductVariantPrices;

namespace StoreManagement.Api.Services.Pricing;

public class ProductVariantPriceService
{
    private readonly ApplicationDbContext _db;

    public ProductVariantPriceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProductVariantPriceResponse?> GetCurrentPriceAsync(
        int productVariantId,
        string priceType = "REGULAR",
        DateTime? asOfUtc = null)
    {
        var effectiveDate = asOfUtc ?? DateTime.UtcNow;

        return await _db.ProductVariantPrices
            .AsNoTracking()
            .Where(x =>
                x.ProductVariantId == productVariantId &&
                x.IsActive &&
                x.PriceType == priceType &&
                x.EffectiveFrom <= effectiveDate &&
                (x.EffectiveTo == null ||
                 x.EffectiveTo >= effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new ProductVariantPriceResponse
            {
                Id = x.Id,
                ProductVariantId = x.ProductVariantId,
                SKU = x.ProductVariant.SKU,
                Barcode = x.ProductVariant.Barcode,
                VariantName = x.ProductVariant.VariantName,
                Price = x.Price,
                PriceType = x.PriceType,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }
}