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

    public BrandsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BrandResponse>>> GetBrands(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Brands.AsNoTracking();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

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
            return NotFound(new { message = "Brand not found." });

        return Ok(brand);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<BrandResponse>> CreateBrand(
        CreateBrandRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _db.Brands
            .AnyAsync(x => x.Name == name);

        if (exists)
            return Conflict(new
            {
                message = $"Brand '{name}' already exists."
            });

        var brand = new Brand
        {
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBrand),
            new { id = brand.Id },
            new BrandResponse
            {
                Id = brand.Id,
                Name = brand.Name,
                IsActive = brand.IsActive,
                CreatedAtUtc = brand.CreatedAtUtc
            });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<BrandResponse>> UpdateBrand(
        int id,
        UpdateBrandRequest request)
    {
        var brand = await _db.Brands.FindAsync(id);

        if (brand == null)
            return NotFound(new { message = "Brand not found." });

        var name = request.Name.Trim();

        var duplicate = await _db.Brands
            .AnyAsync(x => x.Id != id && x.Name == name);

        if (duplicate)
            return Conflict(new
            {
                message = $"Brand '{name}' already exists."
            });

        brand.Name = name;
        brand.UpdatedAtUtc = DateTime.UtcNow;

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

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateBrandStatus(
        int id,
        UpdateBrandStatusRequest request)
    {
        var brand = await _db.Brands.FindAsync(id);

        if (brand == null)
            return NotFound(new { message = "Brand not found." });

        brand.IsActive = request.IsActive;
        brand.UpdatedAtUtc = DateTime.UtcNow;

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