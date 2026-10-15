
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InventoryController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/inventory?storeId=1
    [HttpGet]
    public async Task<IActionResult> GetInventory(
        [FromQuery] int storeId)
    {
        if (storeId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid storeId is required."
            });
        }

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .OfType<ApplicationUser>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "The user does not exist or is inactive."
            });
        }

        // Store-scoped users may only view their own store.
        // Unscoped users must have the SystemAdmin role.
        if (user.StoreId.HasValue)
        {
            if (user.StoreId.Value != storeId)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole("SystemAdmin"))
        {
            return Forbid();
        }

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == storeId);

        if (!storeExists)
        {
            return NotFound(new
            {
                message = "Store not found."
            });
        }

        var inventory = await _db.Set<StoreManagement.Api.Models.InventoryTransaction>()
            .AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .GroupBy(x => x.ProductVariantId)
            .Select(g => new
            {
                ProductVariantId = g.Key,
                CurrentStock = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.ProductVariantId)
            .ToListAsync();

        return Ok(new
        {
            StoreId = storeId,
            Inventory = inventory
        });
    }
}
