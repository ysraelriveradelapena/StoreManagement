namespace StoreManagement.Api.DTOs.Transactions;

public class SaleLineResponse
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? VariantName { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? LocationCode { get; set; }
}