using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Transactions;

public class CreateSaleRequest
{
    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }

    public string? UserId { get; set; }

    public int? DeviceId { get; set; }

    public DateTime TransactionDateUtc { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Remarks { get; set; }

    [MinLength(1)]
    public List<CreateSaleLineRequest> Lines { get; set; } = new();
}