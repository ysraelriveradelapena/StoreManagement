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

    // GET: api/reason-codes
    // GET: api/reason-codes?includeInactive=true
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

    // GET: api/reason-codes/1
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
            return NotFound(new { message = "Reason code not found." });
        }

        return Ok(reasonCode);
    }

    // POST: api/reason-codes
    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ReasonCodeResponse>> CreateReasonCode(
        [FromBody] CreateReasonCodeRequest request)
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

        _db.ReasonCodes.Add(reasonCode);
        await _db.SaveChangesAsync();

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

    // PUT: api/reason-codes/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ReasonCodeResponse>> UpdateReasonCode(
        int id,
        [FromBody] UpdateReasonCodeRequest request)
    {
        var reasonCode = await _db.ReasonCodes.FindAsync(id);

        if (reasonCode == null)
        {
            return NotFound(new { message = "Reason code not found." });
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

        var codeExists = await _db.ReasonCodes
            .AnyAsync(x => x.Id != id && x.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "Another reason code already uses this code."
            });
        }

        reasonCode.Code = code;
        reasonCode.Name = name;

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

    // PATCH: api/reason-codes/1/status
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> UpdateReasonCodeStatus(
        int id,
        [FromBody] UpdateReasonCodeStatusRequest request)
    {
        var reasonCode = await _db.ReasonCodes.FindAsync(id);

        if (reasonCode == null)
        {
            return NotFound(new { message = "Reason code not found." });
        }

        reasonCode.IsActive = request.IsActive;

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