using System.ComponentModel.DataAnnotations;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Models;

public class DeviceRegistrationRequest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    [Required]
    public string RequestedByUserId { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    // Pending, Approved, or Rejected
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }

    public int? DeviceId { get; set; }

    public Store? Store { get; set; }

    public ApplicationUser? RequestedByUser { get; set; }
}