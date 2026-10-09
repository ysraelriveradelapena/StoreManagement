using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Users;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public int? StoreId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}