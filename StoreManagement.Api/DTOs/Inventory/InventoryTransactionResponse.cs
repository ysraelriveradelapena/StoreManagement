namespace StoreManagement.Api.DTOs.Inventory;

public class InventoryTransactionResponse
{
    public long Id { get; set; }

    public int StoreId { get; set; }

    public int ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public decimal Quantity { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public string? LocationCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}