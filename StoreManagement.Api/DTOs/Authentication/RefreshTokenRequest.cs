using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Authentication;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}