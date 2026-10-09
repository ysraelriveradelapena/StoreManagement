namespace StoreManagement.Api.DTOs.Inventory;

public class InventoryHistoryRequest
{
    public int? StoreId { get; set; }

    public int? ProductVariantId { get; set; }

    public string? MovementType { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}