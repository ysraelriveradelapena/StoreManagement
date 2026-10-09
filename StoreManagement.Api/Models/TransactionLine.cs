namespace StoreManagement.Api.Models;

public class TransactionLine
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? LocationCode { get; set; }

    public string? ReasonCode { get; set; }

    public Transaction? Transaction { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}