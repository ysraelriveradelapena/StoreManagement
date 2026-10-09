namespace StoreManagement.Api.DTOs.Transactions;

public class TransactionHistoryResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<SaleResponse> Items { get; set; } = new();
}