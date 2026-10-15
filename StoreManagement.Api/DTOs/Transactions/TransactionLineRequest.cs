
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Transactions;

public class TransactionLineRequest
{
    [Required]
    public int ProductVariantId { get; set; }

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal UnitPrice { get; set; }

    [MaxLength(50)]
    public string? LocationCode { get; set; }

    [MaxLength(50)]
    public string? ReasonCode { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    // Used for EXCHANGE transactions only.
    // IN  = returned item goes back into stock.
    // OUT = replacement item is deducted from stock.
    [MaxLength(3)]
    public string? InventoryDirection { get; set; }
}
