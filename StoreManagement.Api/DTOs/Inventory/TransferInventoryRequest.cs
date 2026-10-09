using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Inventory;

public class TransferInventoryRequest
{
    [Range(1, int.MaxValue)]
    public int FromStoreId { get; set; }

    [Range(1, int.MaxValue)]
    public int ToStoreId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductVariantId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Quantity { get; set; }

    public string? FromLocationCode { get; set; }

    public string? ToLocationCode { get; set; }
}