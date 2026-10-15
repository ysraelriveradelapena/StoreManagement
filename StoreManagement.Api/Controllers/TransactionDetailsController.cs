
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TransactionDetailsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/transactions/123/details
        [HttpGet("{id:int}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Transaction ID must be greater than zero."
                });
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the authenticated user."
                });
            }

            var user = await _db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            var isSystemAdmin = User.IsInRole("SystemAdmin");

            var query = _db.Transactions
                .AsNoTracking()
                .Where(x => x.Id == id);

            if (user.StoreId.HasValue)
            {
                query = query.Where(
                    x => x.StoreId == user.StoreId.Value);
            }
            else if (!isSystemAdmin)
            {
                return Forbid();
            }

            var transaction = await query
                .Include(x => x.Store)
                .Include(x => x.Device)
                .Include(x => x.TransactionType)
                .Include(x => x.Lines)
                    .ThenInclude(x => x.ProductVariant)
                        .ThenInclude(x => x.Product)
                .SingleOrDefaultAsync();

            if (transaction == null)
            {
                return NotFound(new
                {
                    message = "Transaction not found."
                });
            }

            var result = new
            {
                transaction.Id,
                transaction.TransactionId,

                transaction.StoreId,
                StoreCode = transaction.Store?.StoreCode,
                StoreName = transaction.Store?.StoreName,

                transaction.UserId,

                transaction.DeviceId,
                DeviceCode = transaction.Device?.Code,
                DeviceName = transaction.Device?.Name,

                transaction.TransactionTypeId,
                TransactionTypeCode =
                    transaction.TransactionType?.Code,
                TransactionTypeName =
                    transaction.TransactionType?.Name,

                transaction.TransactionDateUtc,
                transaction.ReceivedAtUtc,
                transaction.ReferenceNumber,
                transaction.CountSessionId,
                transaction.Status,
                transaction.Remarks,

                TotalAmount = transaction.Lines.Sum(x => x.Amount),

                Lines = transaction.Lines
                    .OrderBy(x => x.Id)
                    .Select(line => new
                    {
                        line.Id,
                        line.ProductVariantId,

                        ProductName =
                            line.ProductVariant?.Product?.Name,

                        SKU = line.ProductVariant?.SKU,
                        VariantName = line.ProductVariant?.VariantName,

                        Barcode = line.Barcode,
                        line.Quantity,
                        line.UnitPrice,
                        line.Amount,
                        line.LocationCode,
                        line.ReasonCode,
                        line.Remarks
                    })
                    .ToList()
            };

            return Ok(result);
        }
    }
}
