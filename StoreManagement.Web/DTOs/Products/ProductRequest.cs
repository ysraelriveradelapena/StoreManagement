
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Products;

public class ProductRequest
{
    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public bool NonExchangeable { get; set; } = false;
}
