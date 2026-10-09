using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Users;
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

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers(
        [FromQuery] bool includeInactive = false)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .Include(x => x.Store)
            .ToListAsync();

        var result = new List<UserResponse>();

        foreach (var user in users)
        {
            if (!includeInactive && !user.IsActive)
            {
                continue;
            }

            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                StoreId = user.StoreId,
                StoreCode = user.Store?.StoreCode,
                StoreName = user.Store?.StoreName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            });
        }

        return Ok(result
            .OrderBy(x => x.Email)
            .ToList());
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetUser(string id)
    {
        var user = await _userManager.Users
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
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
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request)
    {
        var email = request.Email.Trim();
        var role = request.Role.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new
            {
                message = "Role is required."
            });
        }

        // Validate role
        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new
            {
                message = $"Role '{role}' does not exist."
            });
        }

        // Validate store
        StoreManagement.Api.Models.Store? store = null;

        if (request.StoreId.HasValue)
        {
            store = await _db.Stores
                .FirstOrDefaultAsync(x => x.Id == request.StoreId.Value);

            if (store == null)
            {
                return BadRequest(new
                {
                    message = "Specified store does not exist."
                });
            }

            if (!store.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign a user to an inactive store."
                });
            }
        }

        // Check duplicate email
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = $"A user with email '{email}' already exists."
            });
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
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to create user.",
                errors = createResult.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role);

        if (!roleResult.Succeeded)
        {
            // Remove the user if role assignment fails.
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                message = "Unable to assign role.",
                errors = roleResult.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName ?? string.Empty,
                StoreId = user.StoreId,
                StoreCode = store?.StoreCode,
                StoreName = store?.StoreName,
                Role = role,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            });
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        string id,
        UpdateUserRequest request)
    {
        var user = await _userManager.Users
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var email = request.Email.Trim();
        var role = request.Role.Trim();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new
            {
                message = $"Role '{role}' does not exist."
            });
        }

        if (request.StoreId.HasValue)
        {
            var store = await _db.Stores
                .FirstOrDefaultAsync(x => x.Id == request.StoreId.Value);

            if (store == null)
            {
                return BadRequest(new
                {
                    message = "Specified store does not exist."
                });
            }

            if (!store.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign a user to an inactive store."
                });
            }
        }

        var duplicateEmail = await _userManager.Users
            .AnyAsync(x =>
                x.Id != id &&
                x.NormalizedEmail == email.ToUpper());

        if (duplicateEmail)
        {
            return Conflict(new
            {
                message = $"A user with email '{email}' already exists."
            });
        }

        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = email.ToUpper();
        user.NormalizedUserName = email.ToUpper();
        user.StoreId = request.StoreId;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to update user.",
                errors = updateResult.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(
                user,
                currentRoles);

            if (!removeResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Unable to update user roles.",
                    errors = removeResult.Errors
                        .Select(x => x.Description)
                        .ToList()
                });
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(
            user,
            role);

        if (!addRoleResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to assign role.",
                errors = addRoleResult.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        await _db.Entry(user)
            .Reference(x => x.Store)
            .LoadAsync();

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            StoreId = user.StoreId,
            StoreCode = user.Store?.StoreCode,
            StoreName = user.Store?.StoreName,
            Role = role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });
    }

    // GET: api/users/roles
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetRoles()
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .Select(x => x.Name!)
            .Where(x => x != null)
            .OrderBy(x => x)
            .ToListAsync();

        return Ok(roles);
    }

    // PATCH: api/users/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        string id,
        UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to update user status.",
                errors = result.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        return Ok(new
        {
            message = request.IsActive
                ? "User activated."
                : "User deactivated.",
            userId = user.Id,
            isActive = user.IsActive
        });
    }

    // POST: api/users/{id}/reset-password
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        string id,
        ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to reset password.",
                errors = result.Errors
                    .Select(x => x.Description)
                    .ToList()
            });
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }
}