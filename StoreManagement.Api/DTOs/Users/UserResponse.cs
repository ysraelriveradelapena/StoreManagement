namespace StoreManagement.Api.DTOs.Users;

public class UserResponse
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public int? StoreId { get; set; }

    public string? StoreCode { get; set; }

    public string? StoreName { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}