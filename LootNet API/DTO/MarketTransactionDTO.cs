namespace LootNet_API.DTO;

public class MarketTransactionDTO
{
    public Guid TransactionId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Price { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSale { get; set; }
    public string CounterpartyUsername { get; set; } = "";
}
