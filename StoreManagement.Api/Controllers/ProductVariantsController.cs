using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.ProductVariants;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/product-variants")]
[Authorize]
public class ProductVariantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductVariantsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetProductVariants(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? productId = null)
    {
        IQueryable<ProductVariant> query = _db.ProductVariants
            .AsNoTracking()
            .Include(x => x.Product);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        var result = await query
            .OrderBy(x => x.SKU)
            .Select(x => new ProductVariantResponse
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.ProductCode,
                ProductName = x.Product.Name,
                SKU = x.SKU,
                Barcode = x.Barcode,
                VariantName = x.VariantName,
                Size = x.Size,
                Color = x.Color,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductVariantResponse>> GetProductVariant(int id)
    {
        var result = await _db.ProductVariants
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductVariantResponse
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.ProductCode,
                ProductName = x.Product.Name,
                SKU = x.SKU,
                Barcode = x.Barcode,
                VariantName = x.VariantName,
                Size = x.Size,
                Color = x.Color,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ProductVariantResponse>> GetByBarcode(
        string barcode)
    {
        barcode = barcode.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            return BadRequest("Barcode is required.");
        }

        var result = await _db.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.Barcode == barcode &&
                x.IsActive &&
                x.Product.IsActive)
            .Select(x => new ProductVariantResponse
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.ProductCode,
                ProductName = x.Product.Name,
                SKU = x.SKU,
                Barcode = x.Barcode,
                VariantName = x.VariantName,
                Size = x.Size,
                Color = x.Color,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            return NotFound(new
            {
                message = "Barcode was not found.",
                barcode
            });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductVariantResponse>> Create(
        CreateProductVariantRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(x =>
                x.Id == request.ProductId &&
                x.IsActive);

        if (product == null)
        {
            return BadRequest("Product does not exist or is inactive.");
        }

        var sku = request.SKU.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest("SKU is required.");
        }

        var skuExists = await _db.ProductVariants
            .AnyAsync(x => x.SKU == sku);

        if (skuExists)
        {
            return Conflict($"SKU '{sku}' already exists.");
        }

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();

        if (barcode != null)
        {
            var barcodeExists = await _db.ProductVariants
                .AnyAsync(x => x.Barcode == barcode);

            if (barcodeExists)
            {
                return Conflict($"Barcode '{barcode}' already exists.");
            }
        }

        var variant = new ProductVariant
        {
            ProductId = request.ProductId,
            SKU = sku,
            Barcode = barcode,
            VariantName = string.IsNullOrWhiteSpace(request.VariantName)
                ? null
                : request.VariantName.Trim(),
            Size = string.IsNullOrWhiteSpace(request.Size)
                ? null
                : request.Size.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color)
                ? null
                : request.Color.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ProductVariants.Add(variant);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProductVariant),
            new { id = variant.Id },
            new ProductVariantResponse
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                SKU = variant.SKU,
                Barcode = variant.Barcode,
                VariantName = variant.VariantName,
                Size = variant.Size,
                Color = variant.Color,
                IsActive = variant.IsActive,
                CreatedAtUtc = variant.CreatedAtUtc,
                UpdatedAtUtc = variant.UpdatedAtUtc
            });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductVariantResponse>> Update(
        int id,
        UpdateProductVariantRequest request)
    {
        var variant = await _db.ProductVariants
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (variant == null)
        {
            return NotFound();
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(x =>
                x.Id == request.ProductId &&
                x.IsActive);

        if (product == null)
        {
            return BadRequest("Product does not exist or is inactive.");
        }

        var sku = request.SKU.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest("SKU is required.");
        }

        var skuExists = await _db.ProductVariants
            .AnyAsync(x =>
                x.Id != id &&
                x.SKU == sku);

        if (skuExists)
        {
            return Conflict($"SKU '{sku}' already exists.");
        }

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();

        if (barcode != null)
        {
            var barcodeExists = await _db.ProductVariants
                .AnyAsync(x =>
                    x.Id != id &&
                    x.Barcode == barcode);

            if (barcodeExists)
            {
                return Conflict($"Barcode '{barcode}' already exists.");
            }
        }

        variant.ProductId = request.ProductId;
        variant.SKU = sku;
        variant.Barcode = barcode;
        variant.VariantName = string.IsNullOrWhiteSpace(request.VariantName)
            ? null
            : request.VariantName.Trim();
        variant.Size = string.IsNullOrWhiteSpace(request.Size)
            ? null
            : request.Size.Trim();
        variant.Color = string.IsNullOrWhiteSpace(request.Color)
            ? null
            : request.Color.Trim();
        variant.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            ProductCode = product.ProductCode,
            ProductName = product.Name,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            VariantName = variant.VariantName,
            Size = variant.Size,
            Color = variant.Color,
            IsActive = variant.IsActive,
            CreatedAtUtc = variant.CreatedAtUtc,
            UpdatedAtUtc = variant.UpdatedAtUtc
        });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateProductVariantStatusRequest request)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(x => x.Id == id);

        if (variant == null)
        {
            return NotFound();
        }

        variant.IsActive = request.IsActive;
        variant.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}