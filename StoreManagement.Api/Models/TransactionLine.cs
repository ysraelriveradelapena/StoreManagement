
using System;

namespace StoreManagement.Api.Models;

public class TransactionLine
{
    public int Id { get; set; }

    // Foreign key to Transaction.Id, not the device-generated GUID.
    public int TransactionId { get; set; }

    // Required when the barcode has been matched to a product variant.
    public int ProductVariantId { get; set; }

    // Snapshot of the scanned barcode for audit/history purposes.
    public string? Barcode { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    // Selling area or stockroom.
    public string? LocationCode { get; set; }

    // Required by business validation for applicable transaction types.
    public string? ReasonCode { get; set; }

    public string? Remarks { get; set; }

    public Transaction? Transaction { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
