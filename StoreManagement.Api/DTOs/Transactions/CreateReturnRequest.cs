using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Transactions;

public class CreateReturnRequest
{
    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }

    public string? UserId { get; set; }

    public int? DeviceId { get; set; }

    public DateTime TransactionDateUtc { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Remarks { get; set; }

    public Guid OriginalTransactionId { get; set; }

    [MinLength(1)]
    public List<CreateReturnLineRequest> Lines { get; set; } = new();
}