using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Inventory;
using System.Data;

namespace StoreManagement.Api.Services.Inventory;

public class InventoryService
{
    private readonly ApplicationDbContext _db;

    public InventoryService(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<long?> ReceiveAsync(
    int storeId,
    int productVariantId,
    decimal quantity,
    string? locationCode)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == storeId);

        if (!storeExists)
            return null;

        var variantExists = await _db.ProductVariants
            .AnyAsync(x =>
                x.Id == productVariantId &&
                x.IsActive);

        if (!variantExists)
            return null;

        var inventoryTransaction = new Models.InventoryTransaction
        {
            StoreId = storeId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            MovementType = "RECEIVE",
            LocationCode = string.IsNullOrWhiteSpace(locationCode)
                ? null
                : locationCode.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(inventoryTransaction);

        await _db.SaveChangesAsync();

        return inventoryTransaction.Id;
    }
    public async Task<InventoryStockResponse?> GetStockAsync(
        int storeId,
        int productVariantId)
    {
        var variant = await _db.ProductVariants
            .AsNoTracking()
            .Where(x => x.Id == productVariantId)
            .Select(x => new
            {
                x.Id,
                x.SKU,
                x.Barcode,
                x.VariantName
            })
            .FirstOrDefaultAsync();

        if (variant == null)
            return null;

        var quantity = await _db.InventoryTransactions
            .Where(x =>
                x.StoreId == storeId &&
                x.ProductVariantId == productVariantId)
            .SumAsync(x => (decimal?)x.Quantity) ?? 0;

        return new InventoryStockResponse
        {
            StoreId = storeId,
            ProductVariantId = productVariantId,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            VariantName = variant.VariantName,
            QuantityOnHand = quantity
        };
    }

    public async Task<(bool Success, string? Error, long? Id)> AdjustAsync(
    int storeId,
    int productVariantId,
    decimal quantity,
    string? locationCode)
    {
        if (quantity == 0)
            return (false, "Adjustment quantity cannot be zero.", null);

        var storeExists = await _db.Stores
            .AnyAsync(x => x.Id == storeId);

        if (!storeExists)
            return (false, "Store does not exist.", null);

        var variantExists = await _db.ProductVariants
            .AnyAsync(x =>
                x.Id == productVariantId &&
                x.IsActive);

        if (!variantExists)
        {
            return (
                false,
                "Product variant does not exist or is inactive.",
                null);
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var currentQuantity = await _db.InventoryTransactions
                .Where(x =>
                    x.StoreId == storeId &&
                    x.ProductVariantId == productVariantId)
                .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            var newQuantity = currentQuantity + quantity;

            if (newQuantity < 0)
            {
                await transaction.RollbackAsync();

                return (
                    false,
                    $"Insufficient stock. Current quantity is {currentQuantity}.",
                    null);
            }

            var inventoryTransaction =
                new Models.InventoryTransaction
                {
                    StoreId = storeId,
                    ProductVariantId = productVariantId,
                    Quantity = quantity,
                    MovementType = "ADJUST",
                    LocationCode =
                        string.IsNullOrWhiteSpace(locationCode)
                            ? null
                            : locationCode.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

            _db.InventoryTransactions.Add(inventoryTransaction);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null, inventoryTransaction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> TransferAsync(
    int fromStoreId,
    int toStoreId,
    int productVariantId,
    decimal quantity,
    string? fromLocationCode,
    string? toLocationCode)
    {
        if (fromStoreId == toStoreId)
            return (false, "Source and destination stores must be different.");

        if (quantity <= 0)
            return (false, "Transfer quantity must be greater than zero.");

        var storesExist = await _db.Stores
            .CountAsync(x =>
                x.Id == fromStoreId ||
                x.Id == toStoreId);

        if (storesExist != 2)
            return (false, "Source or destination store does not exist.");

        var variantExists = await _db.ProductVariants
            .AnyAsync(x =>
                x.Id == productVariantId &&
                x.IsActive);

        if (!variantExists)
            return (
                false,
                "Product variant does not exist or is inactive.");

        await using var transaction =
    await _db.Database.BeginTransactionAsync(
        IsolationLevel.Serializable);

        try
        {
            var currentQuantity = await _db.InventoryTransactions
                .Where(x =>
                    x.StoreId == fromStoreId &&
                    x.ProductVariantId == productVariantId)
                .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            if (currentQuantity < quantity)
            {
                await transaction.RollbackAsync();

                return (
                    false,
                    $"Insufficient stock. Current quantity is {currentQuantity}.");
            }

            var transferOut = new Models.InventoryTransaction
            {
                StoreId = fromStoreId,
                ProductVariantId = productVariantId,
                Quantity = -quantity,
                MovementType = "TRANSFER_OUT",
                LocationCode =
                    string.IsNullOrWhiteSpace(fromLocationCode)
                        ? null
                        : fromLocationCode.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var transferIn = new Models.InventoryTransaction
            {
                StoreId = toStoreId,
                ProductVariantId = productVariantId,
                Quantity = quantity,
                MovementType = "TRANSFER_IN",
                LocationCode =
                    string.IsNullOrWhiteSpace(toLocationCode)
                        ? null
                        : toLocationCode.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.InventoryTransactions.Add(transferOut);
            _db.InventoryTransactions.Add(transferIn);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        try
        {
            var transferOut = new Models.InventoryTransaction
            {
                StoreId = fromStoreId,
                ProductVariantId = productVariantId,
                Quantity = -quantity,
                MovementType = "TRANSFER_OUT",
                LocationCode = string.IsNullOrWhiteSpace(fromLocationCode)
                    ? null
                    : fromLocationCode.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var transferIn = new Models.InventoryTransaction
            {
                StoreId = toStoreId,
                ProductVariantId = productVariantId,
                Quantity = quantity,
                MovementType = "TRANSFER_IN",
                LocationCode = string.IsNullOrWhiteSpace(toLocationCode)
                    ? null
                    : toLocationCode.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.InventoryTransactions.Add(transferOut);
            _db.InventoryTransactions.Add(transferIn);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InventoryHistoryResponse> GetHistoryAsync(
    InventoryHistoryRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? 50
            : request.PageSize;

        if (pageSize > 200)
            pageSize = 200;

        IQueryable<Models.InventoryTransaction> query =
            _db.InventoryTransactions
                .AsNoTracking();

        if (request.StoreId.HasValue)
        {
            query = query.Where(x =>
                x.StoreId == request.StoreId.Value);
        }

        if (request.ProductVariantId.HasValue)
        {
            query = query.Where(x =>
                x.ProductVariantId == request.ProductVariantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.MovementType))
        {
            var movementType = request.MovementType.Trim();

            query = query.Where(x =>
                x.MovementType == movementType);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x =>
                x.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x =>
                x.CreatedAtUtc <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryTransactionResponse
            {
                Id = x.Id,
                StoreId = x.StoreId,
                ProductVariantId = x.ProductVariantId,
                SKU = x.ProductVariant.SKU,
                Barcode = x.ProductVariant.Barcode,
                Quantity = x.Quantity,
                MovementType = x.MovementType,
                LocationCode = x.LocationCode,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return new InventoryHistoryResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
}