using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Categories;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Categories.AsNoTracking();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var result = await query
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse
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
    public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (category == null)
            return NotFound(new { message = "Category not found." });

        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _db.Categories
            .AnyAsync(x => x.Name == name);

        if (exists)
            return Conflict(new
            {
                message = $"Category '{name}' already exists."
            });

        var category = new Category
        {
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedAtUtc = category.CreatedAtUtc
            });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int id,
        UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        var name = request.Name.Trim();

        var duplicate = await _db.Categories
            .AnyAsync(x => x.Id != id && x.Name == name);

        if (duplicate)
            return Conflict(new
            {
                message = $"Category '{name}' already exists."
            });

        category.Name = name;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateCategoryStatus(
        int id,
        UpdateCategoryStatusRequest request)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        category.IsActive = request.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Category activated."
                : "Category deactivated.",
            categoryId = category.Id,
            isActive = category.IsActive
        });
    }
}