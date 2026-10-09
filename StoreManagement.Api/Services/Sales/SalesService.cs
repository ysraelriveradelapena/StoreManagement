using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.ProductVariantPrices;
using StoreManagement.Api.DTOs.Transactions;
using StoreManagement.Api.Models;
using StoreManagement.Api.Services.Pricing;
using System.Data;

namespace StoreManagement.Api.Services.Sales;

public class SalesService
{
    private readonly ApplicationDbContext _db;
    private readonly ProductVariantPriceService _priceService;

    public SalesService(
        ApplicationDbContext db,
        ProductVariantPriceService priceService)
    {
        _db = db;
        _priceService = priceService;
    }

    public async Task<(bool Success, string? Error, SaleResponse? Sale)>
    CreateSaleAsync(CreateSaleRequest request)
    {
        if (request.Lines == null || request.Lines.Count == 0)
        {
            return (false, "At least one sale line is required.", null);
        }

        if (request.Lines.Any(x => x.Quantity <= 0))
        {
            return (false, "All quantities must be greater than zero.", null);
        }

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == request.StoreId);

        if (!storeExists)
        {
            return (false, "Store does not exist.", null);
        }

        var saleType = await _db.TransactionTypes
            .FirstOrDefaultAsync(x =>
                x.Code == "SALE" &&
                x.IsActive);

        if (saleType == null)
        {
            return (false, "Active SALE transaction type does not exist.", null);
        }

        if (!saleType.AffectsInventory)
        {
            return (
                false,
                "SALE transaction type is not configured to affect inventory.",
                null);
        }

        var duplicateVariant = request.Lines
            .GroupBy(x => x.ProductVariantId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateVariant != null)
        {
            return (
                false,
                $"Product variant {duplicateVariant.Key} appears more than once.",
                null);
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        SaleResponse? response = null;
        string? error = null;
        bool success = false;

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var variantIds = request.Lines
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var variants = await _db.ProductVariants
                    .Where(x =>
                        variantIds.Contains(x.Id) &&
                        x.IsActive)
                    .ToListAsync();

                if (variants.Count != variantIds.Count)
                {
                    error =
                        "One or more product variants do not exist or are inactive.";

                    await dbTransaction.RollbackAsync();
                    return;
                }

                var saleLines = new List<TransactionLine>();

                foreach (var requestLine in request.Lines)
                {
                    var variant = variants.First(
                        x => x.Id == requestLine.ProductVariantId);

                    var currentPrice =
                        await _priceService.GetCurrentPriceAsync(
                            variant.Id,
                            "REGULAR",
                            request.TransactionDateUtc);

                    if (currentPrice == null)
                    {
                        error =
                            $"No current REGULAR price found for SKU {variant.SKU}.";

                        await dbTransaction.RollbackAsync();
                        return;
                    }

                    var currentStock =
                        await _db.InventoryTransactions
                            .Where(x =>
                                x.StoreId == request.StoreId &&
                                x.ProductVariantId == variant.Id)
                            .SumAsync(x => (decimal?)x.Quantity) ?? 0;

                    if (currentStock < requestLine.Quantity)
                    {
                        error =
                            $"Insufficient stock for SKU {variant.SKU}. " +
                            $"Available: {currentStock}, " +
                            $"Requested: {requestLine.Quantity}.";

                        await dbTransaction.RollbackAsync();
                        return;
                    }

                    var amount =
                        Math.Round(
                            currentPrice.Price * requestLine.Quantity,
                            2,
                            MidpointRounding.AwayFromZero);

                    saleLines.Add(new TransactionLine
                    {
                        ProductVariantId = variant.Id,
                        Quantity = requestLine.Quantity,
                        UnitPrice = currentPrice.Price,
                        Amount = amount,
                        LocationCode =
                            string.IsNullOrWhiteSpace(requestLine.LocationCode)
                                ? null
                                : requestLine.LocationCode.Trim()
                    });
                }

                var sale = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    TransactionTypeId = saleType.Id,
                    StoreId = request.StoreId,
                    UserId = request.UserId,
                    DeviceId = request.DeviceId,
                    TransactionDateUtc = request.TransactionDateUtc,
                    ReceivedAtUtc = DateTime.UtcNow,
                    ReferenceNumber =
                        string.IsNullOrWhiteSpace(request.ReferenceNumber)
                            ? null
                            : request.ReferenceNumber.Trim(),
                    Status = "POSTED",
                    Remarks = request.Remarks,
                    Lines = saleLines
                };

                _db.Transactions.Add(sale);

                foreach (var line in saleLines)
                {
                    _db.InventoryTransactions.Add(
                        new InventoryTransaction
                        {
                            StoreId = request.StoreId,
                            ProductVariantId = line.ProductVariantId,
                            Quantity = -line.Quantity,
                            MovementType = "SALE",
                            LocationCode = line.LocationCode,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                }

                await _db.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                response = new SaleResponse
                {
                    Id = sale.Id,
                    TransactionId = sale.TransactionId,
                    TransactionTypeId = sale.TransactionTypeId,
                    StoreId = sale.StoreId,
                    UserId = sale.UserId,
                    DeviceId = sale.DeviceId,
                    TransactionDateUtc = sale.TransactionDateUtc,
                    ReceivedAtUtc = sale.ReceivedAtUtc,
                    ReferenceNumber = sale.ReferenceNumber,
                    Status = sale.Status,
                    Remarks = sale.Remarks,
                    TotalAmount = saleLines.Sum(x => x.Amount),
                    Lines = saleLines.Select(x =>
                    {
                        var variant = variants.First(
                            v => v.Id == x.ProductVariantId);

                        return new SaleLineResponse
                        {
                            Id = x.Id,
                            ProductVariantId = x.ProductVariantId,
                            SKU = variant.SKU,
                            Barcode = variant.Barcode,
                            VariantName = variant.VariantName,
                            Quantity = x.Quantity,
                            UnitPrice = x.UnitPrice,
                            Amount = x.Amount,
                            LocationCode = x.LocationCode
                        };
                    }).ToList()
                };

                success = true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });

        if (!success)
        {
            return (false, error, null);
        }

        return (true, null, response);
    }

    public async Task<TransactionHistoryResponse> GetSalesAsync(
    TransactionHistoryRequest request)
    {
        var page = request.Page < 1 ? 1 : request.Page;

        var pageSize = request.PageSize < 1
            ? 50
            : request.PageSize;

        if (pageSize > 200)
            pageSize = 200;

        IQueryable<Transaction> query =
            _db.Transactions
                .AsNoTracking()
                .Where(x => x.TransactionType != null &&
                            x.TransactionType.Code == "SALE");

        if (request.StoreId.HasValue)
        {
            query = query.Where(x =>
                x.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();

            query = query.Where(x =>
                x.Status == status);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x =>
                x.TransactionDateUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x =>
                x.TransactionDateUtc <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(x => x.TransactionDateUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SaleResponse
            {
                Id = x.Id,
                TransactionId = x.TransactionId,
                TransactionTypeId = x.TransactionTypeId,
                StoreId = x.StoreId,
                UserId = x.UserId,
                DeviceId = x.DeviceId,
                TransactionDateUtc = x.TransactionDateUtc,
                ReceivedAtUtc = x.ReceivedAtUtc,
                ReferenceNumber = x.ReferenceNumber,
                Status = x.Status,
                Remarks = x.Remarks,

                TotalAmount = x.Lines
                    .Sum(line => line.Amount),

                Lines = x.Lines
                    .Select(line => new SaleLineResponse
                    {
                        Id = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        SKU = line.ProductVariant!.SKU,
                        Barcode = line.ProductVariant.Barcode,
                        VariantName = line.ProductVariant.VariantName,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        Amount = line.Amount,
                        LocationCode = line.LocationCode
                    })
                    .ToList()
            })
            .ToListAsync();

        return new TransactionHistoryResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = transactions
        };
    }

    public async Task<SaleResponse?> GetSaleAsync(
    Guid transactionId)
    {
        return await _db.Transactions
            .AsNoTracking()
            .Where(x =>
                x.TransactionId == transactionId &&
                x.TransactionType != null &&
                x.TransactionType.Code == "SALE")
            .Select(x => new SaleResponse
            {
                Id = x.Id,
                TransactionId = x.TransactionId,
                TransactionTypeId = x.TransactionTypeId,
                StoreId = x.StoreId,
                UserId = x.UserId,
                DeviceId = x.DeviceId,
                TransactionDateUtc = x.TransactionDateUtc,
                ReceivedAtUtc = x.ReceivedAtUtc,
                ReferenceNumber = x.ReferenceNumber,
                Status = x.Status,
                Remarks = x.Remarks,

                TotalAmount = x.Lines
                    .Sum(line => line.Amount),

                Lines = x.Lines
                    .Select(line => new SaleLineResponse
                    {
                        Id = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        SKU = line.ProductVariant!.SKU,
                        Barcode = line.ProductVariant.Barcode,
                        VariantName = line.ProductVariant.VariantName,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        Amount = line.Amount,
                        LocationCode = line.LocationCode
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)>
    VoidSaleAsync(
        Guid transactionId,
        string? reason)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        var success = false;
        string? error = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var sale = await _db.Transactions
                    .Include(x => x.TransactionType)
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.TransactionId == transactionId &&
                        x.TransactionType != null &&
                        x.TransactionType.Code == "SALE");

                if (sale == null)
                {
                    error = "Sale transaction not found.";

                    await dbTransaction.RollbackAsync();
                    return;
                }

                if (sale.Status != "POSTED")
                {
                    error =
                        $"Sale cannot be voided because its current status is '{sale.Status}'.";

                    await dbTransaction.RollbackAsync();
                    return;
                }

                foreach (var line in sale.Lines)
                {
                    var currentStock =
                        await _db.InventoryTransactions
                            .Where(x =>
                                x.StoreId == sale.StoreId &&
                                x.ProductVariantId == line.ProductVariantId)
                            .SumAsync(x => (decimal?)x.Quantity) ?? 0;

                    // A VOID adds the original sold quantity back.
                    // We don't normally need this check because adding stock
                    // cannot make inventory negative, but we keep the
                    // calculation inside the transaction for consistency.
                    _db.InventoryTransactions.Add(
                        new InventoryTransaction
                        {
                            StoreId = sale.StoreId,
                            ProductVariantId = line.ProductVariantId,
                            Quantity = line.Quantity,
                            MovementType = "VOID",
                            LocationCode = line.LocationCode,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                }

                sale.Status = "VOIDED";

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    sale.Remarks =
                        string.IsNullOrWhiteSpace(sale.Remarks)
                            ? $"VOID: {reason.Trim()}"
                            : $"{sale.Remarks} | VOID: {reason.Trim()}";
                }

                await _db.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                success = true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });

        return (success, error);
    }

    public async Task<(bool Success, string? Error, SaleResponse? Return)>
    CreateReturnAsync(CreateReturnRequest request)
    {
        if (request.Lines == null || request.Lines.Count == 0)
        {
            return (false, "At least one return line is required.", null);
        }

        if (request.Lines.Any(x => x.Quantity <= 0))
        {
            return (false, "All return quantities must be greater than zero.", null);
        }

        if (request.OriginalTransactionId == Guid.Empty)
        {
            return (false, "OriginalTransactionId is required.", null);
        }

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == request.StoreId);

        if (!storeExists)
        {
            return (false, "Store does not exist.", null);
        }

        var returnType = await _db.TransactionTypes
            .FirstOrDefaultAsync(x =>
                x.Code == "RETURN" &&
                x.IsActive);

        if (returnType == null)
        {
            return (false, "Active RETURN transaction type does not exist.", null);
        }

        if (!returnType.AffectsInventory)
        {
            return (
                false,
                "RETURN transaction type is not configured to affect inventory.",
                null);
        }

        var duplicateVariant = request.Lines
            .GroupBy(x => x.ProductVariantId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateVariant != null)
        {
            return (
                false,
                $"Product variant {duplicateVariant.Key} appears more than once.",
                null);
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        SaleResponse? response = null;
        string? error = null;
        bool success = false;

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var originalSale = await _db.Transactions
                    .Include(x => x.TransactionType)
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.TransactionId == request.OriginalTransactionId &&
                        x.TransactionType != null &&
                        x.TransactionType.Code == "SALE");

                if (originalSale == null)
                {
                    error = "Original sale transaction not found.";
                    await dbTransaction.RollbackAsync();
                    return;
                }

                if (originalSale.Status != "POSTED")
                {
                    error =
                        $"Original sale cannot be returned because its status is '{originalSale.Status}'.";

                    await dbTransaction.RollbackAsync();
                    return;
                }

                if (originalSale.StoreId != request.StoreId)
                {
                    error = "Return store does not match the original sale store.";
                    await dbTransaction.RollbackAsync();
                    return;
                }

                var variantIds = request.Lines
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var variants = await _db.ProductVariants
                    .Where(x => variantIds.Contains(x.Id))
                    .ToListAsync();

                if (variants.Count != variantIds.Count)
                {
                    error = "One or more product variants do not exist.";
                    await dbTransaction.RollbackAsync();
                    return;
                }

                var returnTransactionIds = await _db.Transactions
                    .Where(x =>
                        x.TransactionType != null &&
                        x.TransactionType.Code == "RETURN" &&
                        x.Remarks != null &&
                        x.Remarks.Contains(
                            request.OriginalTransactionId.ToString()))
                    .Select(x => x.Id)
                    .ToListAsync();

                var alreadyReturned = await _db.TransactionLines
                    .Where(x =>
                        returnTransactionIds.Contains(x.TransactionId))
                    .GroupBy(x => x.ProductVariantId)
                    .Select(x => new
                    {
                        ProductVariantId = x.Key,
                        Quantity = x.Sum(y => y.Quantity)
                    })
                    .ToListAsync();

                var returnLines = new List<TransactionLine>();

                foreach (var requestLine in request.Lines)
                {
                    var originalLine = originalSale.Lines
                        .FirstOrDefault(x =>
                            x.ProductVariantId == requestLine.ProductVariantId);

                    if (originalLine == null)
                    {
                        error =
                            $"Product variant {requestLine.ProductVariantId} " +
                            "was not part of the original sale.";

                        await dbTransaction.RollbackAsync();
                        return;
                    }

                    var previouslyReturned =
                        alreadyReturned
                            .FirstOrDefault(x =>
                                x.ProductVariantId ==
                                requestLine.ProductVariantId)
                            ?.Quantity ?? 0;

                    var remaining =
                        originalLine.Quantity - previouslyReturned;

                    if (requestLine.Quantity > remaining)
                    {
                        error =
                            $"Cannot return {requestLine.Quantity} units of " +
                            $"SKU {variants.First(x => x.Id == requestLine.ProductVariantId).SKU}. " +
                            $"Remaining returnable quantity: {remaining}.";

                        await dbTransaction.RollbackAsync();
                        return;
                    }

                    var amount =
                        Math.Round(
                            originalLine.UnitPrice *
                            requestLine.Quantity,
                            2,
                            MidpointRounding.AwayFromZero);

                    returnLines.Add(new TransactionLine
                    {
                        ProductVariantId =
                            requestLine.ProductVariantId,

                        Quantity = requestLine.Quantity,

                        UnitPrice = originalLine.UnitPrice,

                        Amount = amount,

                        LocationCode =
                            string.IsNullOrWhiteSpace(
                                requestLine.LocationCode)
                                ? originalLine.LocationCode
                                : requestLine.LocationCode.Trim(),

                        ReasonCode =
                            string.IsNullOrWhiteSpace(
                                requestLine.ReasonCode)
                                ? null
                                : requestLine.ReasonCode.Trim()
                    });
                }

                var returnTransaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    TransactionTypeId = returnType.Id,
                    StoreId = request.StoreId,
                    UserId = request.UserId,
                    DeviceId = request.DeviceId,
                    TransactionDateUtc = request.TransactionDateUtc,
                    ReceivedAtUtc = DateTime.UtcNow,
                    ReferenceNumber =
                        string.IsNullOrWhiteSpace(request.ReferenceNumber)
                            ? null
                            : request.ReferenceNumber.Trim(),
                    Status = "POSTED",
                    Remarks =
                        $"RETURN OF {request.OriginalTransactionId}" +
                        (string.IsNullOrWhiteSpace(request.Remarks)
                            ? string.Empty
                            : $" | {request.Remarks.Trim()}"),
                    Lines = returnLines
                };

                _db.Transactions.Add(returnTransaction);

                foreach (var line in returnLines)
                {
                    _db.InventoryTransactions.Add(
                        new InventoryTransaction
                        {
                            StoreId = request.StoreId,
                            ProductVariantId = line.ProductVariantId,
                            Quantity = line.Quantity,
                            MovementType = "RETURN",
                            LocationCode = line.LocationCode,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                }

                await _db.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                response = new SaleResponse
                {
                    Id = returnTransaction.Id,
                    TransactionId = returnTransaction.TransactionId,
                    TransactionTypeId =
                        returnTransaction.TransactionTypeId,
                    StoreId = returnTransaction.StoreId,
                    UserId = returnTransaction.UserId,
                    DeviceId = returnTransaction.DeviceId,
                    TransactionDateUtc =
                        returnTransaction.TransactionDateUtc,
                    ReceivedAtUtc =
                        returnTransaction.ReceivedAtUtc,
                    ReferenceNumber =
                        returnTransaction.ReferenceNumber,
                    Status = returnTransaction.Status,
                    Remarks = returnTransaction.Remarks,
                    TotalAmount = returnLines.Sum(x => x.Amount),
                    Lines = returnLines.Select(x =>
                    {
                        var variant = variants.First(
                            v => v.Id == x.ProductVariantId);

                        return new SaleLineResponse
                        {
                            Id = x.Id,
                            ProductVariantId =
                                x.ProductVariantId,
                            SKU = variant.SKU,
                            Barcode = variant.Barcode,
                            VariantName = variant.VariantName,
                            Quantity = x.Quantity,
                            UnitPrice = x.UnitPrice,
                            Amount = x.Amount,
                            LocationCode = x.LocationCode
                        };
                    }).ToList()
                };

                success = true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });

        if (!success)
        {
            return (false, error, null);
        }

        return (true, null, response);
    }
}