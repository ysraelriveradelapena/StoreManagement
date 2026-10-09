using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.Models;

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(100)]
    public string? VariantName { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<TransactionLine> TransactionLines { get; set; }
        = new List<TransactionLine>();

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
        = new List<InventoryTransaction>();

    public ICollection<ProductVariantPrice> Prices { get; set; }
    = new List<ProductVariantPrice>();
}