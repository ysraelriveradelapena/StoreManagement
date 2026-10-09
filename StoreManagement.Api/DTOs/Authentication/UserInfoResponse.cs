namespace StoreManagement.Api.DTOs.Authentication;

public class UserInfoResponse
{
    public string UserId { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? UserName { get; set; }

    public int? StoreId { get; set; }

    public bool IsActive { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();
}