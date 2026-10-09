using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Authentication;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Services.Authentication;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public TokenService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<LoginResponse> CreateTokensAsync(
        ApplicationUser user,
        string? ipAddress)
    {
        var accessToken = await CreateAccessTokenAsync(user);

        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken.Token),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshTokenEntity);

        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,

            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,

            User = await BuildUserInfoAsync(user)
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress)
    {
        var tokenHash = HashToken(refreshToken);

        var existingToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (existingToken == null)
        {
            return null;
        }

        if (!existingToken.IsActive)
        {
            return null;
        }

        var user = existingToken.User;

        if (user == null || !user.IsActive)
        {
            return null;
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;

        var newRefreshToken = GenerateRefreshToken();

        var newRefreshTokenHash =
            HashToken(newRefreshToken.Token);

        existingToken.ReplacedByTokenHash =
            newRefreshTokenHash;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,

            TokenHash = newRefreshTokenHash,

            CreatedAtUtc = DateTime.UtcNow,

            ExpiresAtUtc =
                newRefreshToken.ExpiresAtUtc,

            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(newRefreshTokenEntity);

        var accessToken =
            await CreateAccessTokenAsync(user);

        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken.Token,

            AccessTokenExpiresAtUtc =
                accessToken.ExpiresAtUtc,

            RefreshToken =
                newRefreshToken.Token,

            RefreshTokenExpiresAtUtc =
                newRefreshToken.ExpiresAtUtc,

            User =
                await BuildUserInfoAsync(user)
        };
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash);

        if (token == null)
        {
            return false;
        }

        if (token.RevokedAtUtc.HasValue)
        {
            return false;
        }

        token.RevokedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<UserInfoResponse> BuildUserInfoAsync(
        ApplicationUser user)
    {
        var roles =
            await _userManager.GetRolesAsync(user);

        return new UserInfoResponse
        {
            UserId = user.Id,

            Email = user.Email,

            UserName = user.UserName,

            StoreId = user.StoreId,

            IsActive = user.IsActive,

            Roles = roles
        };
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)>
        CreateAccessTokenAsync(ApplicationUser user)
    {
        var jwtSection =
            _configuration.GetSection("Jwt");

        var issuer =
            jwtSection["Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is not configured.");

        var audience =
            jwtSection["Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is not configured.");

        var key =
            jwtSection["Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is not configured.");

        var accessTokenMinutes =
            jwtSection.GetValue<int?>(
                "AccessTokenMinutes") ?? 60;

        var roles =
            await _userManager.GetRolesAsync(user);

        var expiresAtUtc =
            DateTime.UtcNow.AddMinutes(
                accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),

            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.UserName ?? string.Empty)
        };

        if (user.StoreId.HasValue)
        {
            claims.Add(
                new Claim(
                    "store_id",
                    user.StoreId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    "role",
                    role));
        }

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (
            new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAtUtc);
    }

    private static (
        string Token,
        DateTime ExpiresAtUtc)
        GenerateRefreshToken()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(64);

        var token =
            Convert.ToBase64String(bytes);

        var expiresAtUtc =
            DateTime.UtcNow.AddDays(30);

        return (
            token,
            expiresAtUtc);
    }

    private static string HashToken(
        string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}