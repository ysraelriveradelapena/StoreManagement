using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.ReasonCodes;

public class ReasonCodeRequest
{
    [Required(ErrorMessage = "Code is required.")]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}