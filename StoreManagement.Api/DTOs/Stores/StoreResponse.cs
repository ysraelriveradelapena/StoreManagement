namespace StoreManagement.Api.DTOs.Stores;

public class StoreResponse
{
    public int Id { get; set; }

    public string StoreCode { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string? DepartmentStore { get; set; }

    public string? Location { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}