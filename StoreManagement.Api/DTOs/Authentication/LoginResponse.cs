namespace StoreManagement.Api.DTOs.Authentication;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public UserInfoResponse User { get; set; } = new();
}