namespace StoreManagement.Web.DTOs.ReasonCodes;

public class ReasonCodeDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}