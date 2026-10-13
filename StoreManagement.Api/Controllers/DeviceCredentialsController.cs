using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Devices;
using StoreManagement.Api.Models;
using StoreManagement.Api.Services.Devices;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Roles = "SystemAdmin")]
public class DeviceCredentialsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DeviceCredentialsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("{id:int}/credential")]
    public async Task<ActionResult<DeviceCredentialResponse>>
        CreateOrRotateCredential(int id)
    {
        var device = await _db.Devices.FindAsync(id);

        if (device == null)
        {
            return NotFound(new
            {
                message = "Device not found."
            });
        }

        var oldValues = new
        {
            device.CredentialCreatedAtUtc,
            HadCredential = !string.IsNullOrWhiteSpace(
                device.CredentialHash)
        };

        var secret = DeviceSecurity.GenerateSecret();

        device.CredentialHash =
            DeviceSecurity.HashSecret(secret);

        device.CredentialCreatedAtUtc = DateTime.UtcNow;

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = "Device",
            EntityId = device.Id.ToString(),
            Action = "ROTATE_CREDENTIAL",
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(new
            {
                device.Code,
                device.CredentialCreatedAtUtc,
                HasCredential = true
            }),
            IpAddress = HttpContext.Connection.RemoteIpAddress?
                .ToString()
        });

        await _db.SaveChangesAsync();

        return Ok(new DeviceCredentialResponse
        {
            DeviceId = device.Id,
            DeviceCode = device.Code,
            DeviceSecret = secret,
            Message =
                "Save this secret securely. It cannot be retrieved later."
        });
    }
}