using Microsoft.AspNetCore.Identity;
using StoreManagement.Api.Models;

namespace StoreManagement.Api.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public int? StoreId { get; set; }

    public Store? Store { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}