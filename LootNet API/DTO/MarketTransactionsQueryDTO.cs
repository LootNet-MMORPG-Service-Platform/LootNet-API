namespace LootNet_API.DTO;

public class MarketTransactionsQueryDTO
{
    public string? Search { get; set; }
    public bool? IsSale { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public RangeFilter<int>? Price { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
