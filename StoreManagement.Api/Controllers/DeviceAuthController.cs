using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StoreManagement.Api.Data;
using StoreManagement.Api.DTOs.Devices;
using StoreManagement.Api.Services.Devices;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/device-auth")]
public class DeviceAuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public DeviceAuthController(
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> CreateToken(
        [FromBody] DeviceTokenRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var device = await _db.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code);

        if (device == null ||
            !device.IsActive ||
            string.IsNullOrWhiteSpace(device.CredentialHash) ||
            !DeviceSecurity.VerifySecret(
                request.DeviceSecret,
                device.CredentialHash))
        {
            return Unauthorized(new
            {
                message = "Invalid device credentials or inactive device."
            });
        }

        var jwtSection = _configuration.GetSection("Jwt");

        var key = jwtSection["DeviceKey"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:DeviceKey must be configured with at least 32 characters.");
        }

        var issuer = jwtSection["DeviceIssuer"]
            ?? "StoreManagement.Device";

        var audience = jwtSection["DeviceAudience"]
            ?? "StoreManagement.DeviceClients";

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        var claims = new[]
        {
            new Claim("device_id", device.Id.ToString()),
            new Claim("device_code", device.Code),
            new Claim("device_access", "true")
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            tokenType = "Bearer",
            expiresAtUtc,
            expiresIn = 900,
            deviceId = device.Id,
            deviceCode = device.Code,
            storeId = device.StoreId
        });
    }
}