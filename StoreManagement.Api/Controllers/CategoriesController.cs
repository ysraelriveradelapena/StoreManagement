
using System.Security.Claims;
using System.Text.Json;
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
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ApplicationDbContext db,
        ILogger<CategoriesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // AUDIT LOG HELPER

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        int categoryId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "Category",
            EntityId = categoryId.ToString(),
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

    // GET ALL CATEGORIES

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Categories.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

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

    // GET CATEGORY BY ID

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
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    // CREATE CATEGORY
    // The configured execution strategy wraps the entire transaction.

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        [FromBody] CreateCategoryRequest request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        if (await _db.Categories.AnyAsync(x => x.Name == name))
        {
            return Conflict(new
            {
                message = $"Category '{name}' already exists."
            });
        }

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            var category = await strategy.ExecuteAsync(async () =>
            {
                // Avoid reusing tracked entities after a retry.
                _db.ChangeTracker.Clear();

                await using var transaction =
                    await _db.Database.BeginTransactionAsync();

                try
                {
                    var entity = new Category
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.Categories.Add(entity);

                    // Generate the database ID.
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

                    // Save the audit record in the same transaction.
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
                            "Rollback failed while creating category {CategoryName}.",
                            name);
                    }

                    _db.ChangeTracker.Clear();
                    throw;
                }
            });

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    IsActive = category.IsActive,
                    CreatedAtUtc = category.CreatedAtUtc,
                    UpdatedAtUtc = category.UpdatedAtUtc
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create category. CategoryName={CategoryName}",
                name);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Unable to create the category. Please try again."
                });
        }
    }

    // UPDATE CATEGORY

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        var duplicate = await _db.Categories
            .AnyAsync(x => x.Id != id && x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Category '{name}' already exists."
            });
        }

        if (category.Name == name)
        {
            return Ok(new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedAtUtc = category.CreatedAtUtc,
                UpdatedAtUtc = category.UpdatedAtUtc
            });
        }

        var oldValues = new
        {
            category.Name
        };

        category.Name = name;
        category.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            category.Id,
            "UPDATE",
            oldValues,
            new
            {
                category.Name
            }));

        // The entity update and audit insert are saved together.
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

    // ACTIVATE / DEACTIVATE CATEGORY

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateCategoryStatus(
        int id,
        [FromBody] UpdateCategoryStatusRequest request)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (category.IsActive == request.IsActive)
        {
            return Ok(new
            {
                message = "Category status is already up to date.",
                categoryId = category.Id,
                isActive = category.IsActive
            });
        }

        var oldValues = new
        {
            category.IsActive
        };

        category.IsActive = request.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            category.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new
            {
                category.IsActive
            }));

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
