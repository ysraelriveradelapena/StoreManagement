
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Devices;
using StoreManagement.Api.Hubs;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;
using StoreManagement.Api.Services.Devices;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/device-registration-requests")]
[Authorize]
public class DeviceRegistrationRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<DeviceNotificationHub> _hubContext;
    private readonly ILogger<DeviceRegistrationRequestsController> _logger;

    public DeviceRegistrationRequestsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IHubContext<DeviceNotificationHub> hubContext,
        ILogger<DeviceRegistrationRequestsController> logger)
    {
        _db = db;
        _userManager = userManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    // ------------------------------------------------------------
    // SIGNALR NOTIFICATION
    // ------------------------------------------------------------

    private async Task NotifyAdminsAsync()
    {
        try
        {
            await _hubContext.Clients
                .Group(DeviceNotificationHub.SystemAdminsGroup)
                .SendAsync("DeviceRegistrationRequestsChanged");
        }
        catch (Exception ex)
        {
            // A notification failure must not undo a successful
            // registration, approval, or rejection.
            _logger.LogError(
                ex,
                "Failed to send device registration notification.");
        }
    }

    // ------------------------------------------------------------
    // SUBMIT REGISTRATION REQUEST
    // ------------------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] DeviceRegistrationSubmitRequest request)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || !user.IsActive || user.StoreId == null)
            return Forbid();

        var store = await _db.Stores
            .FirstOrDefaultAsync(x => x.Id == user.StoreId.Value);

        if (store == null || !store.IsActive)
        {
            return BadRequest(new
            {
                message = "Your assigned store is inactive or unavailable."
            });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Device code and name are required."
            });
        }

        if (await _db.Devices.AnyAsync(x => x.Code == code))
        {
            return Conflict(new
            {
                message = "A device with this code already exists."
            });
        }

        if (await _db.DeviceRegistrationRequests.AnyAsync(x =>
                x.Code == code && x.Status == "Pending"))
        {
            return Conflict(new
            {
                message =
                    "A registration request with this code is already pending."
            });
        }

        var registration = new DeviceRegistrationRequest
        {
            Code = code,
            Name = name,
            StoreId = store.Id,
            RequestedByUserId = user.Id,
            RequestedAtUtc = DateTime.UtcNow,
            Status = "Pending"
        };

        _db.DeviceRegistrationRequests.Add(registration);

        await _db.SaveChangesAsync();

        // Notify connected System Admins immediately.
        await NotifyAdminsAsync();

        return Ok(new
        {
            registration.Id,
            registration.Code,
            registration.Name,
            registration.StoreId,
            registration.Status,
            registration.RequestedAtUtc,
            message = "Device registration request submitted for approval."
        });
    }

    // ------------------------------------------------------------
    // GET REGISTRATION REQUESTS
    // ------------------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<
        IEnumerable<DeviceRegistrationRequestResponse>>> GetRequests(
        [FromQuery] string status = "Pending")
    {
        status = (status ?? "Pending").Trim();

        var query = _db.DeviceRegistrationRequests
            .AsNoTracking()
            .Include(x => x.Store)
            .AsQueryable();

        if (!string.Equals(
                status,
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.ToLower();

            query = query.Where(x =>
                x.Status.ToLower() == normalizedStatus);
        }

        var result = await query
            .OrderByDescending(x => x.RequestedAtUtc)
            .Select(x => new DeviceRegistrationRequestResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                StoreId = x.StoreId,
                StoreName = x.Store != null
                    ? x.Store.StoreName
                    : string.Empty,
                Status = x.Status,
                RequestedAtUtc = x.RequestedAtUtc,
                RequestedByUserId = x.RequestedByUserId
            })
            .ToListAsync();

        return Ok(result);
    }

    // ------------------------------------------------------------
    // APPROVE REGISTRATION REQUEST
    // ------------------------------------------------------------

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Approve(int id)
    {
        var registration = await _db.DeviceRegistrationRequests
            .FirstOrDefaultAsync(x => x.Id == id);

        if (registration == null)
            return NotFound(new { message = "Request not found." });

        if (registration.Status != "Pending")
            return BadRequest(new { message = "Request is not pending." });

        if (!await _db.Stores.AnyAsync(x =>
                x.Id == registration.StoreId && x.IsActive))
        {
            return BadRequest(new
            {
                message = "The requested store is inactive or unavailable."
            });
        }

        if (await _db.Devices.AnyAsync(x => x.Code == registration.Code))
        {
            return Conflict(new
            {
                message = "A device with this code already exists."
            });
        }

        var secret = DeviceSecurity.GenerateSecret();

        var device = new Device
        {
            Code = registration.Code,
            Name = registration.Name,
            StoreId = registration.StoreId,
            IsActive = true,
            CredentialHash = DeviceSecurity.HashSecret(secret),
            CredentialCreatedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        registration.Status = "Approved";
        registration.ReviewedByUserId = CurrentUserId;
        registration.ReviewedAtUtc = DateTime.UtcNow;

        _db.Devices.Add(device);

        await _db.SaveChangesAsync();

        registration.DeviceId = device.Id;

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = "DeviceRegistrationRequest",
            EntityId = registration.Id.ToString(),
            Action = "APPROVE",
            UserId = CurrentUserId,
            CreatedAtUtc = DateTime.UtcNow,
            NewValues = JsonSerializer.Serialize(new
            {
                registration.Code,
                registration.StoreId,
                registration.DeviceId,
                registration.Status
            }),
            IpAddress = HttpContext.Connection.RemoteIpAddress?
                .ToString()
        });

        await _db.SaveChangesAsync();

        // Update the pending-request count for connected admins.
        await NotifyAdminsAsync();

        return Ok(new
        {
            requestId = registration.Id,
            deviceId = device.Id,
            deviceCode = device.Code,
            deviceSecret = secret,
            message = "Device approved. Save the secret securely."
        });
    }

    // ------------------------------------------------------------
    // REJECT REGISTRATION REQUEST
    // ------------------------------------------------------------

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectDeviceRegistrationRequest request)
    {
        var registration = await _db.DeviceRegistrationRequests
            .FirstOrDefaultAsync(x => x.Id == id);

        if (registration == null)
            return NotFound(new { message = "Request not found." });

        if (registration.Status != "Pending")
            return BadRequest(new { message = "Request is not pending." });

        registration.Status = "Rejected";
        registration.ReviewedByUserId = CurrentUserId;
        registration.ReviewedAtUtc = DateTime.UtcNow;
        registration.ReviewNotes = request.Notes?.Trim();

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = "DeviceRegistrationRequest",
            EntityId = registration.Id.ToString(),
            Action = "REJECT",
            UserId = CurrentUserId,
            CreatedAtUtc = DateTime.UtcNow,
            NewValues = JsonSerializer.Serialize(new
            {
                registration.Code,
                registration.StoreId,
                registration.Status,
                registration.ReviewNotes
            }),
            IpAddress = HttpContext.Connection.RemoteIpAddress?
                .ToString()
        });

        await _db.SaveChangesAsync();

        // Update the pending-request count for connected admins.
        await NotifyAdminsAsync();

        return Ok(new
        {
            message = "Device registration request rejected."
        });
    }
}
