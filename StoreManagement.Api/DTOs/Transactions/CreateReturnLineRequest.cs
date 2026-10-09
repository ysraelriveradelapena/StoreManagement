using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Transactions;

public class CreateReturnLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductVariantId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Quantity { get; set; }

    public string? LocationCode { get; set; }

    public string? ReasonCode { get; set; }
}