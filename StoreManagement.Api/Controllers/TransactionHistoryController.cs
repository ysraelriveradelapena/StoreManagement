
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TransactionHistoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/transactions
        //
        // Optional query parameters:
        // storeId, fromUtc, toUtc, transactionTypeCode, status,
        // page, pageSize
        //
        // Example:
        // GET /api/transactions?fromUtc=2026-10-01T00:00:00Z
        //     &toUtc=2026-10-16T00:00:00Z&page=1&pageSize=20

        [HttpGet]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? storeId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? transactionTypeCode,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1)
            {
                return BadRequest(new
                {
                    message = "Page must be greater than zero."
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page size must be between 1 and 100."
                });
            }

            if (fromUtc.HasValue &&
                toUtc.HasValue &&
                fromUtc.Value >= toUtc.Value)
            {
                return BadRequest(new
                {
                    message = "fromUtc must be earlier than toUtc."
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

            // Users assigned to a store may only view that store's
            // transactions. System administrators may filter by store
            // or view transactions across stores.
            if (user.StoreId.HasValue)
            {
                if (storeId.HasValue && storeId.Value != user.StoreId.Value)
                {
                    return Forbid();
                }

                storeId = user.StoreId.Value;
            }
            else if (!isSystemAdmin)
            {
                return Forbid();
            }

            IQueryable<Transaction> query = _db.Transactions
                .AsNoTracking()
                .Include(x => x.Store)
                .Include(x => x.Device)
                .Include(x => x.TransactionType)
                .Include(x => x.Lines)
                    .ThenInclude(x => x.ProductVariant)
                        .ThenInclude(x => x.Product);

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            // Inclusive starting timestamp.
            if (fromUtc.HasValue)
            {
                query = query.Where(
                    x => x.TransactionDateUtc >= fromUtc.Value);
            }

            // Exclusive ending timestamp.
            // For a full date range, use the next day's midnight as toUtc.
            if (toUtc.HasValue)
            {
                query = query.Where(
                    x => x.TransactionDateUtc < toUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(transactionTypeCode))
            {
                var typeCode = transactionTypeCode.Trim();

                query = query.Where(x =>
                    x.TransactionType != null &&
                    x.TransactionType.Code == typeCode);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var transactionStatus = status.Trim();

                query = query.Where(x =>
                    x.Status == transactionStatus);
            }

            var totalCount = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            var transactions = await query
                .OrderByDescending(x => x.TransactionDateUtc)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var results = transactions.Select(x =>
                new TransactionHistoryItemResponse
                {
                    Id = x.Id,
                    TransactionId = x.TransactionId,
                    StoreId = x.StoreId,
                    StoreCode = x.Store != null
                        ? x.Store.StoreCode
                        : null,
                    StoreName = x.Store != null
                        ? x.Store.StoreName
                        : null,

                    UserId = x.UserId,
                    DeviceId = x.DeviceId,
                    DeviceCode = x.Device != null
                        ? x.Device.Code
                        : null,

                    TransactionTypeId = x.TransactionTypeId,
                    TransactionTypeCode = x.TransactionType != null
                        ? x.TransactionType.Code
                        : null,
                    TransactionTypeName = x.TransactionType != null
                        ? x.TransactionType.Name
                        : null,

                    TransactionDateUtc = x.TransactionDateUtc,
                    ReceivedAtUtc = x.ReceivedAtUtc,
                    ReferenceNumber = x.ReferenceNumber,
                    CountSessionId = x.CountSessionId,
                    Status = x.Status,
                    Remarks = x.Remarks,

                    TotalAmount = x.Lines.Sum(line => line.Amount),

                    Lines = x.Lines
                        .OrderBy(line => line.Id)
                        .Select(line =>
                            new TransactionHistoryLineResponse
                            {
                                Id = line.Id,
                                ProductVariantId = line.ProductVariantId,

                                ProductName =
                                    line.ProductVariant != null &&
                                    line.ProductVariant.Product != null
                                        ? line.ProductVariant.Product.Name
                                        : null,

                                SKU = line.ProductVariant != null
                                    ? line.ProductVariant.SKU
                                    : null,

                                Barcode = line.Barcode,

                                VariantName = line.ProductVariant != null
                                    ? line.ProductVariant.VariantName
                                    : null,

                                Quantity = line.Quantity,
                                UnitPrice = line.UnitPrice,
                                Amount = line.Amount,
                                LocationCode = line.LocationCode,
                                ReasonCode = line.ReasonCode,
                                Remarks = line.Remarks
                            })
                        .ToList()
                })
                .ToList();

            return Ok(new TransactionHistoryResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages,
                Items = results
            });
        }
    }

    public class TransactionHistoryResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public List<TransactionHistoryItemResponse> Items { get; set; }
            = new List<TransactionHistoryItemResponse>();
    }

    public class TransactionHistoryItemResponse
    {
        public int Id { get; set; }
        public Guid TransactionId { get; set; }

        public int StoreId { get; set; }
        public string? StoreCode { get; set; }
        public string? StoreName { get; set; }

        public string? UserId { get; set; }

        public int? DeviceId { get; set; }
        public string? DeviceCode { get; set; }

        public int TransactionTypeId { get; set; }
        public string? TransactionTypeCode { get; set; }
        public string? TransactionTypeName { get; set; }

        public DateTime TransactionDateUtc { get; set; }
        public DateTime ReceivedAtUtc { get; set; }

        public string? ReferenceNumber { get; set; }
        public Guid? CountSessionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        public decimal TotalAmount { get; set; }

        public List<TransactionHistoryLineResponse> Lines { get; set; }
            = new List<TransactionHistoryLineResponse>();
    }

    public class TransactionHistoryLineResponse
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }

        public string? ProductName { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public string? VariantName { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }

        public string? LocationCode { get; set; }
        public string? ReasonCode { get; set; }
        public string? Remarks { get; set; }
    }
}
