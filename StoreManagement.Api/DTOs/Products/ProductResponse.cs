
namespace StoreManagement.Api.DTOs.Products;

public class ProductResponse
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? BrandId { get; set; }

    public string? BrandName { get; set; }

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public bool NonExchangeable { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
