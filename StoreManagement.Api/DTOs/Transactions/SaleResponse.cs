namespace StoreManagement.Api.DTOs.Transactions;

public class SaleResponse
{
    public int Id { get; set; }

    public Guid TransactionId { get; set; }

    public int TransactionTypeId { get; set; }

    public int StoreId { get; set; }

    public string? UserId { get; set; }

    public int? DeviceId { get; set; }

    public DateTime TransactionDateUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public string? ReferenceNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public decimal TotalAmount { get; set; }

    public List<SaleLineResponse> Lines { get; set; } = new();
}