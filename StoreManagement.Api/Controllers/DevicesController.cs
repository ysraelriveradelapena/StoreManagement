
using System.Security.Claims;
using System.Text.Json;
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

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        int deviceId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "Device",
            EntityId = deviceId.ToString(),
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

    private async Task<DeviceResponse> BuildDeviceResponse(Device device)
    {
        var storeName = await _db.Stores
            .Where(x => x.Id == device.StoreId)
            .Select(x => x.StoreName)
            .FirstOrDefaultAsync();

        return new DeviceResponse
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
        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

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

        // Save first so the database-generated device ID is available.
        await _db.SaveChangesAsync();

        _db.Set<AuditLog>().Add(CreateAuditLog(
            device.Id,
            "CREATE",
            null,
            new
            {
                device.Code,
                device.Name,
                device.StoreId,
                device.IsActive
            }));

        await _db.SaveChangesAsync();

        var response = await BuildDeviceResponse(device);

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

        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

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

        var oldValues = new
        {
            device.Code,
            device.Name,
            device.StoreId
        };

        var codeChanged = device.Code != code;
        var nameChanged = device.Name != name;
        var storeChanged = device.StoreId != request.StoreId;

        if (!codeChanged && !nameChanged && !storeChanged)
        {
            return Ok(await BuildDeviceResponse(device));
        }

        device.Code = code;
        device.Name = name;
        device.StoreId = request.StoreId;

        _db.Set<AuditLog>().Add(CreateAuditLog(
            device.Id,
            "UPDATE",
            oldValues,
            new
            {
                device.Code,
                device.Name,
                device.StoreId
            }));

        // The device update and audit insert are saved together.
        await _db.SaveChangesAsync();

        return Ok(await BuildDeviceResponse(device));
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

        if (device.IsActive == request.IsActive)
        {
            return Ok(new
            {
                device.Id,
                device.IsActive,
                message = "Device status is unchanged."
            });
        }

        var oldValues = new
        {
            device.IsActive
        };

        device.IsActive = request.IsActive;

        _db.Set<AuditLog>().Add(CreateAuditLog(
            device.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new
            {
                device.IsActive
            }));

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
