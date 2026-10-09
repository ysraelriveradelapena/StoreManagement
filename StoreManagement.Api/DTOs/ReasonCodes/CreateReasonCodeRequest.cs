using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.ReasonCodes;

public class CreateReasonCodeRequest
{
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}