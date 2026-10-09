namespace StoreManagement.Api.DTOs.Inventory;

public class InventoryStockResponse
{
    public int StoreId { get; set; }

    public int ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? VariantName { get; set; }

    public decimal QuantityOnHand { get; set; }
}