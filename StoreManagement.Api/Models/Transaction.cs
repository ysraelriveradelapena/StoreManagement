namespace StoreManagement.Api.Models;

public class Transaction
{
    public int Id { get; set; }

    public Guid TransactionId { get; set; }

    public int TransactionTypeId { get; set; }

    public int StoreId { get; set; }

    public string? UserId { get; set; }

    public int? DeviceId { get; set; }

    public DateTime TransactionDateUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public string? ReferenceNumber { get; set; }

    public string Status { get; set; } = "POSTED";

    public string? Remarks { get; set; }

    public TransactionType? TransactionType { get; set; }

    public Store? Store { get; set; }

    public Device? Device { get; set; }

    public ICollection<TransactionLine> Lines { get; set; }
        = new List<TransactionLine>();
}