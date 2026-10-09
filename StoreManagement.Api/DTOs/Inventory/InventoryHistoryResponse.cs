namespace StoreManagement.Api.DTOs.Inventory;

public class InventoryHistoryResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<InventoryTransactionResponse> Items { get; set; }
        = new();
}