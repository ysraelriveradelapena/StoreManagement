
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Stores;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StoresController> _logger;

    public StoresController(
        ApplicationDbContext db,
        ILogger<StoresController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // AUDIT LOG HELPERS

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        int storeId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "Store",
            EntityId = storeId.ToString(),
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

    // RESPONSE HELPER

    private static StoreResponse ToResponse(Store store)
    {
        return new StoreResponse
        {
            Id = store.Id,
            StoreCode = store.StoreCode,
            StoreName = store.StoreName,
            DepartmentStore = store.DepartmentStore,
            Location = store.Location,
            IsActive = store.IsActive,
            CreatedAtUtc = store.CreatedAtUtc,
            UpdatedAtUtc = store.UpdatedAtUtc
        };
    }

    // GET ALL STORES

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreResponse>>> GetStores(
        [FromQuery] bool includeInactive = false)
    {
        IQueryable<Store> query = _db.Stores.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var stores = await query
            .OrderBy(x => x.StoreCode)
            .Select(x => new StoreResponse
            {
                Id = x.Id,
                StoreCode = x.StoreCode,
                StoreName = x.StoreName,
                DepartmentStore = x.DepartmentStore,
                Location = x.Location,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(stores);
    }

    // GET STORE BY ID

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StoreResponse>> GetStore(int id)
    {
        var store = await _db.Stores
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StoreResponse
            {
                Id = x.Id,
                StoreCode = x.StoreCode,
                StoreName = x.StoreName,
                DepartmentStore = x.DepartmentStore,
                Location = x.Location,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (store == null)
        {
            return NotFound(new
            {
                message = "Store not found."
            });
        }

        return Ok(store);
    }

    // CREATE STORE
    // Uses EF Core's configured execution strategy because SQL Server
    // retry support does not allow a user-created transaction outside it.

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<StoreResponse>> CreateStore(
        [FromBody] CreateStoreRequest request)
    {
        var storeCode = request.StoreCode?.Trim();
        var storeName = request.StoreName?.Trim();

        if (string.IsNullOrWhiteSpace(storeCode))
        {
            return BadRequest(new
            {
                message = "Store code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(storeName))
        {
            return BadRequest(new
            {
                message = "Store name is required."
            });
        }

        if (await _db.Stores.AnyAsync(x => x.StoreCode == storeCode))
        {
            return Conflict(new
            {
                message = $"Store code '{storeCode}' already exists."
            });
        }

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            var store = await strategy.ExecuteAsync(async () =>
            {
                // Reset tracked state before an execution-strategy retry.
                _db.ChangeTracker.Clear();

                await using var transaction =
                    await _db.Database.BeginTransactionAsync();

                try
                {
                    var entity = new Store
                    {
                        StoreCode = storeCode,
                        StoreName = storeName,
                        DepartmentStore =
                            string.IsNullOrWhiteSpace(request.DepartmentStore)
                                ? null
                                : request.DepartmentStore.Trim(),
                        Location =
                            string.IsNullOrWhiteSpace(request.Location)
                                ? null
                                : request.Location.Trim(),
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.Stores.Add(entity);

                    // Save first so the database-generated ID is available.
                    await _db.SaveChangesAsync();

                    _db.AuditLogs.Add(CreateAuditLog(
                        entity.Id,
                        "CREATE",
                        null,
                        new
                        {
                            entity.StoreCode,
                            entity.StoreName,
                            entity.DepartmentStore,
                            entity.Location,
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
                            "Rollback failed while creating store {StoreCode}.",
                            storeCode);
                    }

                    _db.ChangeTracker.Clear();
                    throw;
                }
            });

            return CreatedAtAction(
                nameof(GetStore),
                new { id = store.Id },
                ToResponse(store));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create store. StoreCode={StoreCode}",
                storeCode);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Unable to create the store. Please try again."
                });
        }
    }

    // UPDATE STORE

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<StoreResponse>> UpdateStore(
        int id,
        [FromBody] UpdateStoreRequest request)
    {
        var store = await _db.Stores
            .FirstOrDefaultAsync(x => x.Id == id);

        if (store == null)
        {
            return NotFound(new
            {
                message = "Store not found."
            });
        }

        var storeCode = request.StoreCode?.Trim();
        var storeName = request.StoreName?.Trim();

        if (string.IsNullOrWhiteSpace(storeCode))
        {
            return BadRequest(new
            {
                message = "Store code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(storeName))
        {
            return BadRequest(new
            {
                message = "Store name is required."
            });
        }

        var duplicate = await _db.Stores
            .AnyAsync(x => x.Id != id && x.StoreCode == storeCode);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Store code '{storeCode}' already exists."
            });
        }

        var departmentStore =
            string.IsNullOrWhiteSpace(request.DepartmentStore)
                ? null
                : request.DepartmentStore.Trim();

        var location =
            string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim();

        if (store.StoreCode == storeCode &&
            store.StoreName == storeName &&
            store.DepartmentStore == departmentStore &&
            store.Location == location)
        {
            return Ok(ToResponse(store));
        }

        var oldValues = new
        {
            store.StoreCode,
            store.StoreName,
            store.DepartmentStore,
            store.Location
        };

        store.StoreCode = storeCode;
        store.StoreName = storeName;
        store.DepartmentStore = departmentStore;
        store.Location = location;
        store.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            store.Id,
            "UPDATE",
            oldValues,
            new
            {
                store.StoreCode,
                store.StoreName,
                store.DepartmentStore,
                store.Location
            }));

        // Both changes are saved in one EF Core transaction.
        await _db.SaveChangesAsync();

        return Ok(ToResponse(store));
    }

    // ACTIVATE / DEACTIVATE STORE

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateStoreStatus(
        int id,
        [FromBody] UpdateStoreStatusRequest request)
    {
        var store = await _db.Stores
            .FirstOrDefaultAsync(x => x.Id == id);

        if (store == null)
        {
            return NotFound(new
            {
                message = "Store not found."
            });
        }

        if (store.IsActive == request.IsActive)
        {
            return Ok(new
            {
                message = "Store status is already up to date.",
                storeId = store.Id,
                isActive = store.IsActive
            });
        }

        var oldValues = new
        {
            store.IsActive
        };

        store.IsActive = request.IsActive;
        store.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(CreateAuditLog(
            store.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new
            {
                store.IsActive
            }));

        // Status change and audit record are saved together.
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Store activated."
                : "Store deactivated.",
            storeId = store.Id,
            isActive = store.IsActive
        });
    }
}
