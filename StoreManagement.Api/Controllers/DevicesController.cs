using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Devices;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DevicesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/devices
    // GET: api/devices?includeInactive=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceResponse>>> GetDevices(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Devices
            .AsNoTracking()
            .Include(x => x.Store)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var devices = await query
            .OrderBy(x => x.Code)
            .Select(x => new DeviceResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                StoreId = x.StoreId,
                StoreName = x.Store != null ? x.Store.StoreName : null,
                IsActive = x.IsActive,
                LastSyncAtUtc = x.LastSyncAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(devices);
    }

    // GET: api/devices/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceResponse>> GetDevice(int id)
    {
        var device = await _db.Devices
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DeviceResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                StoreId = x.StoreId,
                StoreName = x.Store != null ? x.Store.StoreName : null,
                IsActive = x.IsActive,
                LastSyncAtUtc = x.LastSyncAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (device == null)
        {
            return NotFound(new { message = "Device not found." });
        }

        return Ok(device);
    }

    // POST: api/devices
    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<DeviceResponse>> CreateDevice(
        [FromBody] CreateDeviceRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == request.StoreId);

        if (!storeExists)
        {
            return BadRequest(new
            {
                message = "The selected store does not exist."
            });
        }

        var codeExists = await _db.Devices
            .AnyAsync(x => x.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "A device with this code already exists."
            });
        }

        var device = new Device
        {
            Code = code,
            Name = name,
            StoreId = request.StoreId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        var storeName = await _db.Stores
            .Where(x => x.Id == device.StoreId)
            .Select(x => x.StoreName)
            .FirstOrDefaultAsync();

        var response = new DeviceResponse
        {
            Id = device.Id,
            Code = device.Code,
            Name = device.Name,
            StoreId = device.StoreId,
            StoreName = storeName,
            IsActive = device.IsActive,
            LastSyncAtUtc = device.LastSyncAtUtc,
            CreatedAtUtc = device.CreatedAtUtc
        };

        return CreatedAtAction(
            nameof(GetDevice),
            new { id = device.Id },
            response);
    }

    // PUT: api/devices/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<DeviceResponse>> UpdateDevice(
        int id,
        [FromBody] UpdateDeviceRequest request)
    {
        var device = await _db.Devices.FindAsync(id);

        if (device == null)
        {
            return NotFound(new { message = "Device not found." });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == request.StoreId);

        if (!storeExists)
        {
            return BadRequest(new
            {
                message = "The selected store does not exist."
            });
        }

        var codeExists = await _db.Devices
            .AnyAsync(x => x.Id != id && x.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "Another device already uses this code."
            });
        }

        device.Code = code;
        device.Name = name;
        device.StoreId = request.StoreId;

        await _db.SaveChangesAsync();

        var storeName = await _db.Stores
            .Where(x => x.Id == device.StoreId)
            .Select(x => x.StoreName)
            .FirstOrDefaultAsync();

        return Ok(new DeviceResponse
        {
            Id = device.Id,
            Code = device.Code,
            Name = device.Name,
            StoreId = device.StoreId,
            StoreName = storeName,
            IsActive = device.IsActive,
            LastSyncAtUtc = device.LastSyncAtUtc,
            CreatedAtUtc = device.CreatedAtUtc
        });
    }

    // PATCH: api/devices/1/status
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateDeviceStatus(
        int id,
        [FromBody] UpdateDeviceStatusRequest request)
    {
        var device = await _db.Devices.FindAsync(id);

        if (device == null)
        {
            return NotFound(new { message = "Device not found." });
        }

        device.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Device activated successfully."
                : "Device deactivated successfully.",
            device.Id,
            device.IsActive
        });
    }
}