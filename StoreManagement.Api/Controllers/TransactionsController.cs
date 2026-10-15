
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Transactions;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TransactionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            if (request == null)
                return BadRequest(new
                {
                    message = "Request body is required."
                });

            if (request.TransactionId == Guid.Empty)
                return BadRequest(new
                {
                    message = "TransactionId must be a valid GUID."
                });

            if (request.StoreId <= 0 || request.DeviceId <= 0)
                return BadRequest(new
                {
                    message = "StoreId and DeviceId must be valid."
                });

            if (request.TransactionDateUtc == default)
                return BadRequest(new
                {
                    message = "TransactionDateUtc is required."
                });

            if (string.IsNullOrWhiteSpace(request.TransactionTypeCode))
                return BadRequest(new
                {
                    message = "TransactionTypeCode is required."
                });

            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest(new
                {
                    message = "At least one transaction line is required."
                });

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _db.Users
                .OfType<ApplicationUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
                return Unauthorized(new
                {
                    message = "The user does not exist or is inactive."
                });

            if (user.StoreId.HasValue)
            {
                if (user.StoreId.Value != request.StoreId)
                    return Forbid();
            }
            else if (!User.IsInRole("SystemAdmin"))
            {
                return Forbid();
            }

            var storeExists = await _db.Stores
                .AnyAsync(x => x.Id == request.StoreId);

            if (!storeExists)
                return NotFound(new
                {
                    message = "Store not found."
                });

            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.Id == request.DeviceId);

            if (device == null || !device.IsActive)
                return BadRequest(new
                {
                    message = "The device does not exist or is inactive."
                });

            if (device.StoreId != request.StoreId)
                return BadRequest(new
                {
                    message = "The device is not assigned to the specified store."
                });

            var transactionTypeCode =
                request.TransactionTypeCode.Trim().ToUpperInvariant();

            var transactionType = await _db.TransactionTypes
                .FirstOrDefaultAsync(x =>
                    x.Code == transactionTypeCode && x.IsActive);

            if (transactionType == null)
                return BadRequest(new
                {
                    message = "Invalid or inactive transaction type."
                });

            var requiresReason = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "RETURN",
                "EXCHANGE",
                "COUNT_LINE",
                "DISCREPANCY",
                "VARIANCE"
            };

            var productVariantIds = request.Lines
                .Select(x => x.ProductVariantId)
                .Distinct()
                .ToList();
            if (transactionTypeCode == "COUNT_LINE")
            {
                if (!transactionType.AffectsInventory)
                {
                    return BadRequest(new
                    {
                        message = "COUNT_LINE must have AffectsInventory enabled."
                    });
                }

                var duplicateVariantIds = request.Lines
                    .GroupBy(x => x.ProductVariantId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateVariantIds.Count > 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "COUNT_LINE can contain only one line per product variant. Submit separate counts if necessary.",
                        productVariantIds = duplicateVariantIds
                    });
                }
            }
            if (productVariantIds.Any(x => x <= 0))
                return BadRequest(new
                {
                    message = "Every line must have a valid ProductVariantId."
                });

            var exchangeHasIn = false;
            var exchangeHasOut = false;

            foreach (var line in request.Lines)
            {
                if (line.Quantity <= 0)
                    return BadRequest(new
                    {
                        message = "Quantity must be greater than zero."
                    });

                if (line.UnitPrice < 0)
                    return BadRequest(new
                    {
                        message = "UnitPrice cannot be negative."
                    });

                if (requiresReason.Contains(transactionTypeCode) &&
                    string.IsNullOrWhiteSpace(line.ReasonCode))
                {
                    return BadRequest(new
                    {
                        message =
                            "A reason code is required for this transaction type."
                    });
                }

                if (!string.IsNullOrWhiteSpace(line.ReasonCode))
                {
                    var reasonCode = line.ReasonCode.Trim();

                    var validReason = await _db.ReasonCodes
                        .AnyAsync(x =>
                            x.Code == reasonCode && x.IsActive);

                    if (!validReason)
                        return BadRequest(new
                        {
                            message =
                                "Invalid or inactive reason code: " + reasonCode
                        });
                }

                if (transactionTypeCode == "EXCHANGE")
                {
                    var direction = line.InventoryDirection?
                        .Trim()
                        .ToUpperInvariant();

                    if (direction != "IN" && direction != "OUT")
                        return BadRequest(new
                        {
                            message =
                                "Every EXCHANGE line must specify InventoryDirection as IN or OUT."
                        });

                    if (direction == "IN")
                        exchangeHasIn = true;

                    if (direction == "OUT")
                        exchangeHasOut = true;
                }
                else if (!string.IsNullOrWhiteSpace(
                    line.InventoryDirection))
                {
                    return BadRequest(new
                    {
                        message =
                            "InventoryDirection can only be supplied for EXCHANGE transactions."
                    });
                }
            }

            if (transactionTypeCode == "EXCHANGE")
            {
                if (!transactionType.AffectsInventory)
                    return BadRequest(new
                    {
                        message =
                            "EXCHANGE must have AffectsInventory enabled."
                    });

                if (request.Lines.Count < 2)
                    return BadRequest(new
                    {
                        message =
                            "An EXCHANGE requires at least two lines: a returned item and a replacement item."
                    });

                if (!exchangeHasIn || !exchangeHasOut)
                    return BadRequest(new
                    {
                        message =
                            "An EXCHANGE must contain at least one IN line and one OUT line."
                    });
            }

            var existingVariants = await _db.ProductVariants
                .Where(x => productVariantIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            var missingVariants = productVariantIds
                .Except(existingVariants)
                .ToList();

            if (missingVariants.Count > 0)
                return BadRequest(new
                {
                    message =
                        "One or more product variants do not exist.",
                    productVariantIds = missingVariants
                });

            var movementDirections =
                new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    { "SALE", -1m },
                    { "DELIVERY", 1m },
                    { "PULLOUT", -1m },
                    { "RETURN", 1m },
                    { "FREE_ITEM", -1m }
                };

            if (transactionType.AffectsInventory &&
                transactionTypeCode != "EXCHANGE" &&
                transactionTypeCode != "COUNT_LINE" &&
                !movementDirections.ContainsKey(transactionTypeCode))
            {
                return BadRequest(new
                {
                    message =
                        "Inventory movement direction is not configured for transaction type: " +
                        transactionTypeCode
                });
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                var existing = await _db.Transactions
                    .Include(x => x.TransactionType)
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(
                        x => x.TransactionId == request.TransactionId);

                if (existing != null)
                {
                    if (existing.StoreId != request.StoreId ||
                        existing.DeviceId != request.DeviceId)
                    {
                        return Conflict(new
                        {
                            message =
                                "TransactionId already exists for another store or device."
                        });
                    }

                    return Ok(BuildResponse(existing, true));
                }

                await using var dbTransaction =
                    await _db.Database.BeginTransactionAsync();

                try
                {
                    var now = DateTime.UtcNow;

                    var transaction = new Models.Transaction
                    {
                        TransactionId = request.TransactionId,
                        StoreId = request.StoreId,
                        DeviceId = request.DeviceId,
                        TransactionTypeId = transactionType.Id,
                        TransactionDateUtc = request.TransactionDateUtc,
                        ReceivedAtUtc = now,
                        ReferenceNumber = request.ReferenceNumber,
                        CountSessionId = request.CountSessionId,
                        Remarks = request.Remarks,
                        Status = transactionTypeCode == "COUNT_LINE"
                            ? "PENDING_APPROVAL"
                            : "POSTED",
                        Lines = new List<TransactionLine>()
                    };

                    foreach (var lineRequest in request.Lines)
                    {
                        var line = new TransactionLine
                        {
                            ProductVariantId =
                                lineRequest.ProductVariantId,

                            Barcode = lineRequest.Barcode,

                            Quantity = lineRequest.Quantity,

                            UnitPrice = lineRequest.UnitPrice,

                            Amount =
                                lineRequest.Quantity *
                                lineRequest.UnitPrice,

                            LocationCode = lineRequest.LocationCode,

                            ReasonCode = string.IsNullOrWhiteSpace(
                                lineRequest.ReasonCode)
                                ? null
                                : lineRequest.ReasonCode.Trim(),

                            Remarks = lineRequest.Remarks
                        };

                        transaction.Lines.Add(line);
                    }

                    _db.Transactions.Add(transaction);

                    // Save the transaction header and lines first.
                    await _db.SaveChangesAsync();

                    // Create inventory movements inside the same DB transaction.

                    // Create inventory movements inside the same DB transaction.
                    if (transactionType.AffectsInventory &&
                        transactionTypeCode != "COUNT_LINE")
                    {
                        var transactionLines = transaction.Lines.ToList();

                        for (var i = 0; i < transactionLines.Count; i++)
                        {
                            var line = transactionLines[i];

                            decimal direction;
                            string movementType;

                            if (transactionTypeCode == "EXCHANGE")
                            {
                                var inventoryDirection =
                                    request.Lines[i].InventoryDirection!
                                        .Trim()
                                        .ToUpperInvariant();

                                direction =
                                    inventoryDirection == "IN" ? 1m : -1m;

                                movementType =
                                    inventoryDirection == "IN"
                                        ? "EXCHANGE_IN"
                                        : "EXCHANGE_OUT";
                            }
                            else
                            {
                                direction =
                                    movementDirections[transactionTypeCode];

                                movementType = transactionTypeCode;
                            }

                            var movement = new InventoryTransaction
                            {
                                TransactionId = transaction.Id,
                                StoreId = transaction.StoreId,
                                ProductVariantId = line.ProductVariantId,
                                Quantity = line.Quantity * direction,
                                MovementType = movementType,
                                LocationCode = line.LocationCode,
                                CreatedAtUtc = now
                            };

                            _db.Set<InventoryTransaction>().Add(movement);
                        }

                        await _db.SaveChangesAsync();
                    }


                    await dbTransaction.CommitAsync();

                    transaction.TransactionType = transactionType;

                    return Ok(BuildResponse(transaction, false));
                }
                catch (DbUpdateException)
                {
                    await dbTransaction.RollbackAsync();

                    _db.ChangeTracker.Clear();

                    var duplicate = await _db.Transactions
                        .Include(x => x.TransactionType)
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(
                            x => x.TransactionId == request.TransactionId);

                    if (duplicate != null)
                    {
                        if (duplicate.StoreId == request.StoreId &&
                            duplicate.DeviceId == request.DeviceId)
                        {
                            return Ok(BuildResponse(duplicate, true));
                        }

                        return Conflict(new
                        {
                            message =
                                "TransactionId already exists for another store or device."
                        });
                    }

                    throw;
                }
            });
        }
        [HttpPost("{id:int}/approve-count")]
        public async Task<IActionResult> ApproveCount(int id)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _db.Users
                .OfType<ApplicationUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
                return Unauthorized(new
                {
                    message = "The user does not exist or is inactive."
                });

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var dbTransaction =
                    await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                var transaction = await _db.Transactions
                    .Include(x => x.TransactionType)
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (transaction == null)
                {
                    await dbTransaction.RollbackAsync();

                    return NotFound(new
                    {
                        message = "Transaction not found."
                    });
                }

                if (user.StoreId.HasValue)
                {
                    if (user.StoreId.Value != transaction.StoreId)
                    {
                        await dbTransaction.RollbackAsync();
                        return Forbid();
                    }
                }
                else if (!User.IsInRole("SystemAdmin"))
                {
                    await dbTransaction.RollbackAsync();
                    return Forbid();
                }

                if (!string.Equals(
                    transaction.TransactionType?.Code,
                    "COUNT_LINE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await dbTransaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Only COUNT_LINE transactions can be approved here."
                    });
                }

                // An already-approved count must never adjust inventory again.
                if (transaction.Status == "APPROVED")
                {
                    await dbTransaction.CommitAsync();

                    return Ok(new
                    {
                        message = "This count has already been approved.",
                        AlreadyApproved = true,
                        Transaction = BuildResponse(transaction, false)
                    });
                }

                if (transaction.Status != "PENDING_APPROVAL")
                {
                    await dbTransaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Only transactions with PENDING_APPROVAL status can be approved.",
                        CurrentStatus = transaction.Status
                    });
                }

                if (transaction.Lines.Count == 0)
                {
                    await dbTransaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "The count contains no transaction lines."
                    });
                }

                var duplicateVariants = transaction.Lines
                    .GroupBy(x => x.ProductVariantId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateVariants.Count > 0)
                {
                    await dbTransaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "The count contains duplicate product variants. Correct the count before approval.",
                        productVariantIds = duplicateVariants
                    });
                }

                var adjustments = new List<object>();
                var now = DateTime.UtcNow;

                foreach (var line in transaction.Lines)
                {
                    // The submitted Quantity is the physical count,
                    // not the amount to add or deduct.
                    var systemStock =
                        await _db.InventoryTransactions
                            .Where(x =>
                                x.StoreId == transaction.StoreId &&
                                x.ProductVariantId == line.ProductVariantId)
                            .SumAsync(x => (decimal?)x.Quantity) ?? 0m;

                    var physicalCount = line.Quantity;
                    var variance = physicalCount - systemStock;

                    if (variance != 0m)
                    {
                        var movement = new InventoryTransaction
                        {
                            TransactionId = transaction.Id,
                            StoreId = transaction.StoreId,
                            ProductVariantId = line.ProductVariantId,
                            Quantity = variance,
                            MovementType = "COUNT_ADJUSTMENT",
                            LocationCode = line.LocationCode,
                            CreatedAtUtc = now
                        };

                        _db.InventoryTransactions.Add(movement);
                    }

                    adjustments.Add(new
                    {
                        line.ProductVariantId,
                        SystemStock = systemStock,
                        PhysicalCount = physicalCount,
                        Variance = variance,
                        NewStock = systemStock + variance,
                        line.LocationCode
                    });
                }

                transaction.Status = "APPROVED";

                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    message = "Physical count approved successfully.",
                    TransactionId = transaction.TransactionId,
                    DatabaseTransactionId = transaction.Id,
                    StoreId = transaction.StoreId,
                    Status = transaction.Status,
                    ApprovedAtUtc = now,
                    Adjustments = adjustments
                });
            });
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var transaction = await _db.Transactions
                .AsNoTracking()
                .Include(x => x.TransactionType)
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (transaction == null)
                return NotFound(new
                {
                    message = "Transaction not found."
                });

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _db.Users
                .OfType<ApplicationUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
                return Unauthorized();

            if (user.StoreId.HasValue)
            {
                if (user.StoreId.Value != transaction.StoreId)
                    return Forbid();
            }
            else if (!User.IsInRole("SystemAdmin"))
            {
                return Forbid();
            }

            return Ok(BuildResponse(transaction, false));
        }

        private static object BuildResponse(
            Models.Transaction transaction,
            bool duplicate)
        {
            var lines = transaction.Lines ??
                new List<TransactionLine>();

            return new
            {
                Id = transaction.Id,
                TransactionId = transaction.TransactionId,
                StoreId = transaction.StoreId,
                DeviceId = transaction.DeviceId,
                TransactionTypeCode = transaction.TransactionType?.Code,
                TransactionTypeName = transaction.TransactionType?.Name,
                TransactionDateUtc = transaction.TransactionDateUtc,
                ReceivedAtUtc = transaction.ReceivedAtUtc,
                ReferenceNumber = transaction.ReferenceNumber,
                CountSessionId = transaction.CountSessionId,
                Status = transaction.Status,
                Remarks = transaction.Remarks,
                Duplicate = duplicate,
                TotalAmount = lines.Sum(x => x.Amount),
                Lines = lines.Select(x => new
                {
                    x.Id,
                    x.ProductVariantId,
                    x.Barcode,
                    x.Quantity,
                    x.UnitPrice,
                    x.Amount,
                    x.LocationCode,
                    x.ReasonCode,
                    x.Remarks
                }).ToList()
            };
        }
    }
}
