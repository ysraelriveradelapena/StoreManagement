
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.ProductVariantPrices;
using StoreManagement.Api.Models;
using StoreManagement.Api.Services.Pricing;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/product-variant-prices")]
[Authorize]
public class ProductVariantPricesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ProductVariantPriceService _priceService;

    public ProductVariantPricesController(
        ApplicationDbContext db,
        ProductVariantPriceService priceService)
    {
        _db = db;
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductVariantPriceResponse>>> GetPrices(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? productVariantId = null)
    {
        IQueryable<ProductVariantPrice> query =
            _db.ProductVariantPrices
                .AsNoTracking()
                .Include(x => x.ProductVariant);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x =>
                x.ProductVariantId == productVariantId.Value);
        }

        var result = await query
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
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductVariantPriceResponse>> GetPrice(
        int id)
    {
        var result = await _db.ProductVariantPrices
            .AsNoTracking()
            .Where(x => x.Id == id)
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

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("current/{productVariantId:int}")]
    public async Task<ActionResult<ProductVariantPriceResponse>> GetCurrentPrice(
        int productVariantId,
        [FromQuery] string priceType = "REGULAR")
    {
        var result = await _priceService.GetCurrentPriceAsync(
            productVariantId,
            priceType);

        if (result == null)
        {
            return NotFound(new
            {
                message = "No current price found for this product variant."
            });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductVariantPriceResponse>> Create(
        CreateProductVariantPriceRequest request)
    {
        if (request.Price < 0)
        {
            return BadRequest("Price cannot be negative.");
        }

        var priceType = request.PriceType.Trim();

        if (string.IsNullOrWhiteSpace(priceType))
        {
            return BadRequest("PriceType is required.");
        }

        if (request.EffectiveTo.HasValue &&
            request.EffectiveTo.Value < request.EffectiveFrom)
        {
            return BadRequest(
                "EffectiveTo cannot be earlier than EffectiveFrom.");
        }

        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(x =>
                x.Id == request.ProductVariantId &&
                x.IsActive);

        if (variant == null)
        {
            return BadRequest(
                "Product variant does not exist or is inactive.");
        }

        var overlapping = await _db.ProductVariantPrices
            .AnyAsync(x =>
                x.ProductVariantId == request.ProductVariantId &&
                x.PriceType == priceType &&
                x.IsActive &&
                request.EffectiveFrom <=
                    (x.EffectiveTo ?? DateTime.MaxValue) &&
                (request.EffectiveTo ?? DateTime.MaxValue) >=
                    x.EffectiveFrom);

        if (overlapping)
        {
            return Conflict(
                "The price period overlaps an existing active price.");
        }

        var price = new ProductVariantPrice
        {
            ProductVariantId = request.ProductVariantId,
            Price = request.Price,
            PriceType = priceType,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true
        };

        _db.ProductVariantPrices.Add(price);

        await _db.SaveChangesAsync();

        var newValues = new
        {
            price.ProductVariantId,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            VariantName = variant.VariantName,
            price.Price,
            price.PriceType,
            price.EffectiveFrom,
            price.EffectiveTo,
            price.IsActive
        };

        _db.AuditLogs.Add(CreateAuditLog(
            price.Id,
            "CREATE",
            null,
            newValues));

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPrice),
            new { id = price.Id },
            new ProductVariantPriceResponse
            {
                Id = price.Id,
                ProductVariantId = price.ProductVariantId,
                SKU = variant.SKU,
                Barcode = variant.Barcode,
                VariantName = variant.VariantName,
                Price = price.Price,
                PriceType = price.PriceType,
                EffectiveFrom = price.EffectiveFrom,
                EffectiveTo = price.EffectiveTo,
                IsActive = price.IsActive
            });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductVariantPriceResponse>> Update(
        int id,
        UpdateProductVariantPriceRequest request)
    {
        var price = await _db.ProductVariantPrices
            .Include(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (price == null)
        {
            return NotFound();
        }

        if (request.Price < 0)
        {
            return BadRequest("Price cannot be negative.");
        }

        var priceType = request.PriceType.Trim();

        if (string.IsNullOrWhiteSpace(priceType))
        {
            return BadRequest("PriceType is required.");
        }

        if (request.EffectiveTo.HasValue &&
            request.EffectiveTo.Value < request.EffectiveFrom)
        {
            return BadRequest(
                "EffectiveTo cannot be earlier than EffectiveFrom.");
        }

        var overlapping = await _db.ProductVariantPrices
            .AnyAsync(x =>
                x.Id != id &&
                x.ProductVariantId == price.ProductVariantId &&
                x.PriceType == priceType &&
                x.IsActive &&
                request.EffectiveFrom <=
                    (x.EffectiveTo ?? DateTime.MaxValue) &&
                (request.EffectiveTo ?? DateTime.MaxValue) >=
                    x.EffectiveFrom);

        if (overlapping)
        {
            return Conflict(
                "The price period overlaps an existing active price.");
        }

        var oldValues = new
        {
            price.ProductVariantId,
            SKU = price.ProductVariant.SKU,
            Barcode = price.ProductVariant.Barcode,
            VariantName = price.ProductVariant.VariantName,
            price.Price,
            price.PriceType,
            price.EffectiveFrom,
            price.EffectiveTo,
            price.IsActive
        };

        var changed =
            price.Price != request.Price ||
            price.PriceType != priceType ||
            price.EffectiveFrom != request.EffectiveFrom ||
            price.EffectiveTo != request.EffectiveTo;

        price.Price = request.Price;
        price.PriceType = priceType;
        price.EffectiveFrom = request.EffectiveFrom;
        price.EffectiveTo = request.EffectiveTo;

        if (changed)
        {
            var newValues = new
            {
                price.ProductVariantId,
                SKU = price.ProductVariant.SKU,
                Barcode = price.ProductVariant.Barcode,
                VariantName = price.ProductVariant.VariantName,
                price.Price,
                price.PriceType,
                price.EffectiveFrom,
                price.EffectiveTo,
                price.IsActive
            };

            _db.AuditLogs.Add(CreateAuditLog(
                price.Id,
                "UPDATE",
                oldValues,
                newValues));

            await _db.SaveChangesAsync();
        }

        return Ok(new ProductVariantPriceResponse
        {
            Id = price.Id,
            ProductVariantId = price.ProductVariantId,
            SKU = price.ProductVariant.SKU,
            Barcode = price.ProductVariant.Barcode,
            VariantName = price.ProductVariant.VariantName,
            Price = price.Price,
            PriceType = price.PriceType,
            EffectiveFrom = price.EffectiveFrom,
            EffectiveTo = price.EffectiveTo,
            IsActive = price.IsActive
        });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateProductVariantPriceStatusRequest request)
    {
        var price = await _db.ProductVariantPrices
            .Include(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (price == null)
        {
            return NotFound();
        }

        if (price.IsActive != request.IsActive)
        {
            var oldValues = new
            {
                price.IsActive,
                price.Price,
                price.PriceType,
                price.EffectiveFrom,
                price.EffectiveTo
            };

            price.IsActive = request.IsActive;

            var newValues = new
            {
                price.IsActive,
                price.Price,
                price.PriceType,
                price.EffectiveFrom,
                price.EffectiveTo
            };

            var action = request.IsActive
                ? "ACTIVATE"
                : "DEACTIVATE";

            _db.AuditLogs.Add(CreateAuditLog(
                price.Id,
                action,
                oldValues,
                newValues));

            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    private AuditLog CreateAuditLog(
        int priceId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "ProductVariantPrice",
            EntityId = priceId.ToString(),
            Action = action,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CreatedAtUtc = DateTime.UtcNow,
            OldValues = oldValues == null
                ? null
                : JsonSerializer.Serialize(oldValues),
            NewValues = newValues == null
                ? null
                : JsonSerializer.Serialize(newValues),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
    }
}