using StoreManagement.Api.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.Models;

public class Store
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StoreCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DepartmentStore { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ApplicationUser> Users { get; set; }
        = new List<ApplicationUser>();
}