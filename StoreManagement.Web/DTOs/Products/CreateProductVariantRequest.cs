namespace StoreManagement.Web.DTOs.Products;

public class CreateProductVariantRequest
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = "";

    public string Barcode { get; set; } = "";

    public string VariantName { get; set; } = "";

    public string? Size { get; set; }

    public string? Color { get; set; }
}