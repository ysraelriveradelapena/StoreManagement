
namespace StoreManagement.Web.DTOs.Brands;

public class BrandDto
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
