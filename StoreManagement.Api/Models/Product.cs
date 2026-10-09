using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? BrandId { get; set; }

    public Brand? Brand { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ProductVariant> Variants { get; set; }
        = new List<ProductVariant>();
}