
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Brands;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize]
public class BrandsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(
        ApplicationDbContext db,
        ILogger<BrandsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        int brandId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "Brand",
            EntityId = brandId.ToString(),
            Action = action,
            UserId = CurrentUserId,
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

    // GET ALL BRANDS

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BrandResponse>>> GetBrands(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Brands.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var result = await query
            .OrderBy(x => x.Name)
            .Select(x => new BrandResponse
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET BRAND BY ID

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BrandResponse>> GetBrand(int id)
    {
        var brand = await _db.Brands
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BrandResponse
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (brand == null)
        {
            return NotFound(new
            {
                message = "Brand not found."
            });
        }

        return Ok(brand);
    }

    // CREATE BRAND
    // The execution strategy wraps the complete transaction so it can
    // retry database operations when the configured SQL strategy allows it.

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<BrandResponse>> CreateBrand(
        [FromBody] CreateBrandRequest request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Brand name is required."
            });
        }

        if (await _db.Brands.AnyAsync(x => x.Name == name))
        {
            return Conflict(new
            {
                message = $"Brand '{name}' already exists."
            });
        }

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            var brand = await strategy.ExecuteAsync(async () =>
            {
                // Reset tracked entities before a possible retry.
                _db.ChangeTracker.Clear();

                await using var transaction =
                    await _db.Database.BeginTransactionAsync();

                try
                {
                    var entity = new Brand
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.Brands.Add(entity);

                    // Save first to obtain the database-generated ID.
                    await _db.SaveChangesAsync();

                    _db.AuditLogs.Add(CreateAuditLog(
                        entity.Id,
                        "CREATE",
                        null,
                        new
                        {
                            entity.Name,
                            entity.IsActive
                        }));

                    // Save the audit record inside the same transaction.
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return entity;
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackException)
                    {
                        _logger.LogWarning(
                            rollbackException,
                            "Rollback failed while creating brand {BrandName}.",
                            name);
                    }

                    _db.ChangeTracker.Clear();
                    throw;
                }
            });

            return CreatedAtAction(
                nameof(GetBrand),
                new { id = brand.Id },
                new BrandResponse
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    IsActive = brand.IsActive,
                    CreatedAtUtc = brand.CreatedAtUtc,
                    UpdatedAtUtc = brand.UpdatedAtUtc
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create brand. BrandName={BrandName}",
                name);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Unable to create the brand. Please try again."
                });
        }
    }

    // UPDATE BRAND

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<BrandResponse>> UpdateBrand(
        int id,
        [FromBody] UpdateBrandRequest request)
    {
        var brand = await _db.Brands.FindAsync(id);

        if (brand == null)
        {
            return NotFound(new
            {
                message = "Brand not found."
            });
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Brand name is required."
            });
        }

        var duplicate = await _db.Brands
            .AnyAsync(x => x.Id != id && x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Brand '{name}' already exists."
            });
        }

        if (brand.Name == name)
        {
            return Ok(new BrandResponse
            {
                Id = brand.Id,
                Name = brand.Name,
                IsActive = brand.IsActive,
                CreatedAtUtc = brand.CreatedAtUtc,
                UpdatedAtUtc = brand.UpdatedAtUtc
            });
        }

        var oldValues = new
        {
            brand.Name
        };

        brand.Name = name;
        brand.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            brand.Id,
            "UPDATE",
            oldValues,
            new
            {
                brand.Name
            }));

        // Brand update and audit record are saved together.
        await _db.SaveChangesAsync();

        return Ok(new BrandResponse
        {
            Id = brand.Id,
            Name = brand.Name,
            IsActive = brand.IsActive,
            CreatedAtUtc = brand.CreatedAtUtc,
            UpdatedAtUtc = brand.UpdatedAtUtc
        });
    }

    // ACTIVATE / DEACTIVATE BRAND

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateBrandStatus(
        int id,
        [FromBody] UpdateBrandStatusRequest request)
    {
        var brand = await _db.Brands.FindAsync(id);

        if (brand == null)
        {
            return NotFound(new
            {
                message = "Brand not found."
            });
        }

        if (brand.IsActive == request.IsActive)
        {
            return Ok(new
            {
                message = "Brand status is already up to date.",
                brandId = brand.Id,
                isActive = brand.IsActive
            });
        }

        var oldValues = new
        {
            brand.IsActive
        };

        brand.IsActive = request.IsActive;
        brand.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            brand.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new
            {
                brand.IsActive
            }));

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Brand activated."
                : "Brand deactivated.",
            brandId = brand.Id,
            isActive = brand.IsActive
        });
    }
}
