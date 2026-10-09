
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Users;

public class ResetUserPasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
