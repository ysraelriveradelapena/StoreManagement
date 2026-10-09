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

    public StoresController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/stores
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreResponse>>> GetStores(
        [FromQuery] bool includeInactive = false)
    {
        IQueryable<Store> query = _db.Stores
            .AsNoTracking();

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

    // GET: api/stores/1
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

    // POST: api/stores
    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<StoreResponse>> CreateStore(
        CreateStoreRequest request)
    {
        var storeCode = request.StoreCode.Trim();

        if (string.IsNullOrWhiteSpace(storeCode))
        {
            return BadRequest(new
            {
                message = "Store code is required."
            });
        }

        var exists = await _db.Stores
            .AnyAsync(x => x.StoreCode == storeCode);

        if (exists)
        {
            return Conflict(new
            {
                message = $"Store code '{storeCode}' already exists."
            });
        }

        var store = new Store
        {
            StoreCode = storeCode,
            StoreName = request.StoreName.Trim(),
            DepartmentStore = string.IsNullOrWhiteSpace(request.DepartmentStore)
                ? null
                : request.DepartmentStore.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Stores.Add(store);

        await _db.SaveChangesAsync();

        var response = new StoreResponse
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

        return CreatedAtAction(
            nameof(GetStore),
            new { id = store.Id },
            response);
    }

    // PUT: api/stores/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<StoreResponse>> UpdateStore(
        int id,
        UpdateStoreRequest request)
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

        var storeCode = request.StoreCode.Trim();

        var duplicate = await _db.Stores
            .AnyAsync(x =>
                x.Id != id &&
                x.StoreCode == storeCode);

        if (duplicate)
        {
            return Conflict(new
            {
                message = $"Store code '{storeCode}' already exists."
            });
        }

        store.StoreCode = storeCode;
        store.StoreName = request.StoreName.Trim();

        store.DepartmentStore =
            string.IsNullOrWhiteSpace(request.DepartmentStore)
                ? null
                : request.DepartmentStore.Trim();

        store.Location =
            string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim();

        store.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new StoreResponse
        {
            Id = store.Id,
            StoreCode = store.StoreCode,
            StoreName = store.StoreName,
            DepartmentStore = store.DepartmentStore,
            Location = store.Location,
            IsActive = store.IsActive,
            CreatedAtUtc = store.CreatedAtUtc,
            UpdatedAtUtc = store.UpdatedAtUtc
        });
    }

    // PATCH: api/stores/1/status
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateStoreStatus(
        int id,
        UpdateStoreStatusRequest request)
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

        store.IsActive = request.IsActive;
        store.UpdatedAtUtc = DateTime.UtcNow;

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