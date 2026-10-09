
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Users;

public class UserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public int? StoreId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}
