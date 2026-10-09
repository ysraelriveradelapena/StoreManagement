namespace StoreManagement.Web.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = "";

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = "";

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public LoginUser User { get; set; } = new();
}

public class LoginUser
{
    public string UserId { get; set; } = "";

    public string Email { get; set; } = "";

    public string UserName { get; set; } = "";

    public string? StoreId { get; set; }

    public bool IsActive { get; set; }

    public List<string> Roles { get; set; } = new();
}