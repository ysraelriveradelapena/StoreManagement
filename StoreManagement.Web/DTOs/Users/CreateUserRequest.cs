
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Users;

public class CreateUserRequest : UserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
