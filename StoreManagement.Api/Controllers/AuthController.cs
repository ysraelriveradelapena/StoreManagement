using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Api.DTOs.Authentication;
using StoreManagement.Api.Models.Identity;
using StoreManagement.Api.Services.Authentication;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var email =
            request.Email.Trim();

        var user =
            await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new
            {
                message = "This user account is inactive."
            });
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!validPassword)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?
                .ToString();

        var response =
            await _tokenService.CreateTokensAsync(
                user,
                ipAddress);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?
                .ToString();

        var response =
            await _tokenService.RefreshTokenAsync(
                request.RefreshToken,
                ipAddress);

        if (response == null)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request)
    {
        var result =
            await _tokenService.RevokeRefreshTokenAsync(
                request.RefreshToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Refresh token not found."
            });
        }

        return Ok(new
        {
            message = "Logged out successfully."
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponse>> Me()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(
                "sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user =
            await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Unauthorized();
        }

        var response =
            await _tokenService.BuildUserInfoAsync(
                user);

        return Ok(response);
    }
}