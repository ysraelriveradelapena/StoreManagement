
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Products;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(
        [FromQuery] bool includeInactive = false)
    {
        IQueryable<Product> query = _db.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var result = await query
            .OrderBy(x => x.ProductCode)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                Name = x.Name,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                NonExchangeable = x.NonExchangeable,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                Name = x.Name,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                NonExchangeable = x.NonExchangeable,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        CreateProductRequest request)
    {
        var productCode = request.ProductCode.Trim();
        var name = request.Name.Trim();

        var duplicate = await _db.Products
            .AnyAsync(x => x.ProductCode == productCode);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Product code '{productCode}' already exists."
            });
        }

        Brand? brand = null;

        if (request.BrandId.HasValue)
        {
            brand = await _db.Brands
                .FirstOrDefaultAsync(x => x.Id == request.BrandId.Value);

            if (brand == null)
            {
                return BadRequest(new
                {
                    message = "Specified brand does not exist."
                });
            }

            if (!brand.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign an inactive brand."
                });
            }
        }

        Category? category = null;

        if (request.CategoryId.HasValue)
        {
            category = await _db.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId.Value);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "Specified category does not exist."
                });
            }

            if (!category.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign an inactive category."
                });
            }
        }

        var product = new Product
        {
            ProductCode = productCode,
            Name = name,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            NonExchangeable = request.NonExchangeable,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);

        await _db.SaveChangesAsync();

        var newValues = new
        {
            product.ProductCode,
            product.Name,
            product.BrandId,
            BrandName = brand?.Name,
            product.CategoryId,
            CategoryName = category?.Name,
            product.NonExchangeable,
            product.IsActive
        };

        _db.AuditLogs.Add(CreateAuditLog(
            product.Id,
            "CREATE",
            null,
            newValues));

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            new ProductResponse
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                Name = product.Name,
                BrandId = product.BrandId,
                BrandName = brand?.Name,
                CategoryId = product.CategoryId,
                CategoryName = category?.Name,
                NonExchangeable = product.NonExchangeable,
                IsActive = product.IsActive,
                CreatedAtUtc = product.CreatedAtUtc,
                UpdatedAtUtc = product.UpdatedAtUtc
            });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id,
        UpdateProductRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        var productCode = request.ProductCode.Trim();
        var name = request.Name.Trim();

        var duplicate = await _db.Products
            .AnyAsync(x =>
                x.Id != id &&
                x.ProductCode == productCode);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Product code '{productCode}' already exists."
            });
        }

        Brand? brand = null;

        if (request.BrandId.HasValue)
        {
            brand = await _db.Brands
                .FirstOrDefaultAsync(x => x.Id == request.BrandId.Value);

            if (brand == null)
            {
                return BadRequest(new
                {
                    message = "Specified brand does not exist."
                });
            }

            if (!brand.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign an inactive brand."
                });
            }
        }

        Category? category = null;

        if (request.CategoryId.HasValue)
        {
            category = await _db.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId.Value);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "Specified category does not exist."
                });
            }

            if (!category.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign an inactive category."
                });
            }
        }

        var oldValues = new
        {
            product.ProductCode,
            product.Name,
            product.BrandId,
            product.CategoryId,
            product.NonExchangeable,
            product.IsActive
        };

        var changed =
            product.ProductCode != productCode ||
            product.Name != name ||
            product.BrandId != request.BrandId ||
            product.CategoryId != request.CategoryId ||
            product.NonExchangeable != request.NonExchangeable;

        product.ProductCode = productCode;
        product.Name = name;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.NonExchangeable = request.NonExchangeable;

        if (changed)
        {
            product.UpdatedAtUtc = DateTime.UtcNow;

            var newValues = new
            {
                product.ProductCode,
                product.Name,
                product.BrandId,
                product.CategoryId,
                product.NonExchangeable,
                product.IsActive
            };

            _db.AuditLogs.Add(CreateAuditLog(
                product.Id,
                "UPDATE",
                oldValues,
                newValues));

            await _db.SaveChangesAsync();
        }

        return Ok(new ProductResponse
        {
            Id = product.Id,
            ProductCode = product.ProductCode,
            Name = product.Name,
            BrandId = product.BrandId,
            BrandName = brand?.Name,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name,
            NonExchangeable = product.NonExchangeable,
            IsActive = product.IsActive,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateProductStatus(
        int id,
        UpdateProductStatusRequest request)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        if (product.IsActive != request.IsActive)
        {
            var oldValues = new
            {
                product.IsActive
            };

            product.IsActive = request.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            var newValues = new
            {
                product.IsActive
            };

            var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";

            _db.AuditLogs.Add(CreateAuditLog(
                product.Id,
                action,
                oldValues,
                newValues));

            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            message = request.IsActive
                ? "Product activated."
                : "Product deactivated.",
            productId = product.Id,
            isActive = product.IsActive
        });
    }

    private AuditLog CreateAuditLog(
        int productId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "Product",
            EntityId = productId.ToString(),
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
