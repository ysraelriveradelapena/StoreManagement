namespace StoreManagement.Api.Models;

public class TransactionType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool AffectsInventory { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();
}