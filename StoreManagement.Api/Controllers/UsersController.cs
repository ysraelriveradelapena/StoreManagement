
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Users;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SystemAdmin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private AuditLog CreateAuditLog(
        string userId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            EntityName = "User",
            EntityId = userId,
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

    // GET: api/users
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.Users
            .Include(u => u.Store)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query
            .OrderBy(u => u.Email)
            .ToListAsync();

        var result = new List<UserResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                StoreId = user.StoreId,
                StoreCode = user.Store?.StoreCode,
                StoreName = user.Store?.StoreName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            });
        }

        return Ok(result);
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            StoreId = user.StoreId,
            StoreCode = user.Store?.StoreCode,
            StoreName = user.Store?.StoreName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { message = "Role is required." });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new { message = "Selected role does not exist." });
        }

        if (request.StoreId.HasValue)
        {
            var storeExists = await _db.Stores.AnyAsync(s =>
                s.Id == request.StoreId.Value && s.IsActive);

            if (!storeExists)
            {
                return BadRequest(new
                {
                    message = "Selected store does not exist or is inactive."
                });
            }
        }

        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            StoreId = request.StoreId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(
            user, request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user, request.Role);

        if (!roleResult.Succeeded)
        {
            // Compensate for the successful user creation.
            var deleteResult = await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
            {
                return StatusCode(500, new
                {
                    message = "The role assignment failed and the newly created user could not be removed.",
                    errors = roleResult.Errors.Select(e => e.Description),
                    cleanupErrors = deleteResult.Errors.Select(e => e.Description)
                });
            }

            return BadRequest(new
            {
                errors = roleResult.Errors.Select(e => e.Description)
            });
        }

        _db.Set<AuditLog>().Add(CreateAuditLog(
            user.Id,
            "CREATE",
            null,
            new
            {
                user.Email,
                user.StoreId,
                Role = request.Role,
                user.IsActive
            }));

        await _db.SaveChangesAsync();

        var store = user.StoreId.HasValue
            ? await _db.Stores.FindAsync(user.StoreId.Value)
            : null;

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                StoreId = user.StoreId,
                StoreCode = store?.StoreCode,
                StoreName = store?.StoreName,
                Role = request.Role,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            });
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { message = "Role is required." });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new { message = "Selected role does not exist." });
        }

        if (request.StoreId.HasValue)
        {
            var storeExists = await _db.Stores.AnyAsync(s =>
                s.Id == request.StoreId.Value && s.IsActive);

            if (!storeExists)
            {
                return BadRequest(new
                {
                    message = "Selected store does not exist or is inactive."
                });
            }
        }

        var email = request.Email.Trim();

        var duplicateEmail = await _userManager.FindByEmailAsync(email);

        if (duplicateEmail != null && duplicateEmail.Id != user.Id)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? string.Empty;

        var oldValues = new
        {
            user.Email,
            user.StoreId,
            Role = oldRole
        };

        var emailChanged = !string.Equals(
            user.Email, email, StringComparison.OrdinalIgnoreCase);

        var storeChanged = user.StoreId != request.StoreId;

        var roleChanged = !string.Equals(
            oldRole, request.Role, StringComparison.Ordinal);

        if (!emailChanged && !storeChanged && !roleChanged)
        {
            return await GetUser(id);
        }

        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = _userManager.NormalizeEmail(email);
        user.NormalizedUserName = _userManager.NormalizeName(email);
        user.StoreId = request.StoreId;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = updateResult.Errors.Select(e => e.Description)
            });
        }

        if (roleChanged)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, currentRoles);

                if (!removeResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        errors = removeResult.Errors.Select(e => e.Description)
                    });
                }
            }

            var addResult = await _userManager.AddToRoleAsync(
                user, request.Role);

            if (!addResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = addResult.Errors.Select(e => e.Description)
                });
            }
        }

        _db.Set<AuditLog>().Add(CreateAuditLog(
            user.Id,
            "UPDATE",
            oldValues,
            new
            {
                user.Email,
                user.StoreId,
                Role = request.Role
            }));

        await _db.SaveChangesAsync();

        await _db.Entry(user).Reference(u => u.Store).LoadAsync();

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            StoreId = user.StoreId,
            StoreCode = user.Store?.StoreCode,
            StoreName = user.Store?.StoreName,
            Role = request.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });
    }

    // GET: api/users/roles
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    // PATCH: api/users/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        string id,
        [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.IsActive == request.IsActive)
        {
            return Ok(new
            {
                user.Id,
                user.IsActive,
                message = "User status is unchanged."
            });
        }

        var oldValues = new { user.IsActive };

        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = updateResult.Errors.Select(e => e.Description)
            });
        }

        _db.Set<AuditLog>().Add(CreateAuditLog(
            user.Id,
            request.IsActive ? "ACTIVATE" : "DEACTIVATE",
            oldValues,
            new { user.IsActive }));

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.IsActive,
            message = request.IsActive
                ? "User activated successfully."
                : "User deactivated successfully."
        });
    }

    // POST: api/users/{id}/reset-password
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        string id,
        [FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetResult = await _userManager.ResetPasswordAsync(
            user, token, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = resetResult.Errors.Select(e => e.Description)
            });
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = updateResult.Errors.Select(e => e.Description)
            });
        }

        _db.Set<AuditLog>().Add(CreateAuditLog(
            user.Id,
            "RESET_PASSWORD",
            null,
            new { PasswordReset = true }));

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }
}
