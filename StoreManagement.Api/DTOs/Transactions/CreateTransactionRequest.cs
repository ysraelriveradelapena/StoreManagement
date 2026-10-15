
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Api.DTOs.Transactions;

public class CreateTransactionRequest
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    public int StoreId { get; set; }

    [Required]
    public int DeviceId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TransactionTypeCode { get; set; } = string.Empty;

    [Required]
    public DateTime TransactionDateUtc { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    public Guid? CountSessionId { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MinLength(1)]
    public List<TransactionLineRequest> Lines { get; set; }
        = new List<TransactionLineRequest>();
}
