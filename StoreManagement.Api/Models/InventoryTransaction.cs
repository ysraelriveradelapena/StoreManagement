namespace StoreManagement.Api.Models;

public class InventoryTransaction
{
    public long Id { get; set; }

    public int? TransactionId { get; set; }

    public int StoreId { get; set; }

    public int ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public string? LocationCode { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Transaction? Transaction { get; set; }

    public Store? Store { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}