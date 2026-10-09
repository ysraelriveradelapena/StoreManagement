using StoreManagement.Api.DTOs.Authentication;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Services.Authentication;

public interface ITokenService
{
    Task<LoginResponse> CreateTokensAsync(
        ApplicationUser user,
        string? ipAddress);

    Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress);

    Task<bool> RevokeRefreshTokenAsync(
        string refreshToken);

    Task<UserInfoResponse> BuildUserInfoAsync(
        ApplicationUser user);
}