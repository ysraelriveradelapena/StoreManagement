using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Inventory;

public class AdjustInventoryRequest
{
    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public string? LocationCode { get; set; }
}