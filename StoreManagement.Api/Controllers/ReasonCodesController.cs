
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.ReasonCodes;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/reason-codes")]
[Authorize]
public class ReasonCodesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReasonCodesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // AUDIT LOG HELPER

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        int reasonCodeId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "ReasonCode",
            EntityId = reasonCodeId.ToString(),
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

    // GET ALL REASON CODES

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReasonCodeResponse>>> GetReasonCodes(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.ReasonCodes.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var result = await query
            .OrderBy(x => x.Code)
            .Select(x => new ReasonCodeResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET REASON CODE BY ID

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReasonCodeResponse>> GetReasonCode(int id)
    {
        var reasonCode = await _db.ReasonCodes
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ReasonCodeResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (reasonCode == null)
        {
            return NotFound(new
            {
                message = "Reason code not found."
            });
        }

        return Ok(reasonCode);
    }

    // CREATE REASON CODE

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ReasonCodeResponse>> CreateReasonCode(
        [FromBody] CreateReasonCodeRequest request)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var codeExists = await _db.ReasonCodes
            .AnyAsync(x => x.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "A reason code with this code already exists."
            });
        }

        var reasonCode = new ReasonCode
        {
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Save the Reason Code and audit record
        // within one database transaction.

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        _db.ReasonCodes.Add(reasonCode);

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(CreateAuditLog(
            reasonCode.Id,
            "CREATE",
            null,
            new
            {
                reasonCode.Code,
                reasonCode.Name,
                reasonCode.IsActive
            }));

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        var response = new ReasonCodeResponse
        {
            Id = reasonCode.Id,
            Code = reasonCode.Code,
            Name = reasonCode.Name,
            IsActive = reasonCode.IsActive,
            CreatedAtUtc = reasonCode.CreatedAtUtc
        };

        return CreatedAtAction(
            nameof(GetReasonCode),
            new { id = reasonCode.Id },
            response);
    }

    // UPDATE REASON CODE

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ReasonCodeResponse>> UpdateReasonCode(
        int id,
        [FromBody] UpdateReasonCodeRequest request)
    {
        var reasonCode = await _db.ReasonCodes.FindAsync(id);

        if (reasonCode == null)
        {
            return NotFound(new
            {
                message = "Reason code not found."
            });
        }

        var code = request.Code?.Trim().ToUpperInvariant();
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Code and Name are required."
            });
        }

        var codeExists = await _db.ReasonCodes
            .AnyAsync(x => x.Id != id && x.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "Another reason code already uses this code."
            });
        }

        // Avoid audit records when nothing has changed.

        if (reasonCode.Code == code &&
            reasonCode.Name == name)
        {
            return Ok(new ReasonCodeResponse
            {
                Id = reasonCode.Id,
                Code = reasonCode.Code,
                Name = reasonCode.Name,
                IsActive = reasonCode.IsActive,
                CreatedAtUtc = reasonCode.CreatedAtUtc
            });
        }

        var oldValues = new
        {
            reasonCode.Code,
            reasonCode.Name
        };

        reasonCode.Code = code;
        reasonCode.Name = name;

        _db.AuditLogs.Add(CreateAuditLog(
            reasonCode.Id,
            "UPDATE",
            oldValues,
            new
            {
                reasonCode.Code,
                reasonCode.Name
            }));

        // Update and audit insert are saved together.

        await _db.SaveChangesAsync();

        return Ok(new ReasonCodeResponse
        {
            Id = reasonCode.Id,
            Code = reasonCode.Code,
            Name = reasonCode.Name,
            IsActive = reasonCode.IsActive,
            CreatedAtUtc = reasonCode.CreatedAtUtc
        });
    }

    // ACTIVATE / DEACTIVATE REASON CODE

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateReasonCodeStatus(
        int id,
        [FromBody] UpdateReasonCodeStatusRequest request)
    {
        var reasonCode = await _db.ReasonCodes.FindAsync(id);

        if (reasonCode == null)
        {
            return NotFound(new
            {
                message = "Reason code not found."
            });
        }

        // Avoid duplicate audit records for unchanged status.

        if (reasonCode.IsActive == request.IsActive)
        {
            return Ok(new
            {
                message = "Reason code status is already up to date.",
                reasonCode.Id,
                reasonCode.IsActive
            });
        }

        var oldValues = new
        {
            reasonCode.IsActive
        };

        reasonCode.IsActive = request.IsActive;

        _db.AuditLogs.Add(CreateAuditLog(
            reasonCode.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new
            {
                reasonCode.IsActive
            }));

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Reason code activated successfully."
                : "Reason code deactivated successfully.",

            reasonCode.Id,
            reasonCode.IsActive
        });
    }
}
