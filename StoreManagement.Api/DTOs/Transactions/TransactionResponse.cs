
using System;
using System.Collections.Generic;

namespace StoreManagement.Api.DTOs.Transactions;

public class TransactionResponse
{
    public int Id { get; set; }

    public Guid TransactionId { get; set; }

    public int StoreId { get; set; }

    public int DeviceId { get; set; }

    public string TransactionTypeCode { get; set; } = string.Empty;

    public string TransactionTypeName { get; set; } = string.Empty;

    public DateTime TransactionDateUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public string? ReferenceNumber { get; set; }

    public Guid? CountSessionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public bool Duplicate { get; set; }

    public decimal TotalAmount { get; set; }

    public List<TransactionLineResponse> Lines { get; set; }
        = new List<TransactionLineResponse>();
}

public class TransactionLineResponse
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    public string? Barcode { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? LocationCode { get; set; }

    public string? ReasonCode { get; set; }

    public string? Remarks { get; set; }
}
