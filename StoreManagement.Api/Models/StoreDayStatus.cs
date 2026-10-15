
using System;

namespace StoreManagement.Api.Models;

public class StoreDayStatus
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    // Store's local business date, not a UTC timestamp.
    public DateTime BusinessDate { get; set; }

    // SALES_SUBMITTED, NO_SALES, DAY_OFF, STORE_CLOSED
    public string Status { get; set; } = "SALES_SUBMITTED";

    public string? Remarks { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public string? SubmittedByUserId { get; set; }

    public Store? Store { get; set; }
}
