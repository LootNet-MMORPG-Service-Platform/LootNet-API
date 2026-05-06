namespace LootNet_API.DTO;

public class MarketTransactionsSummaryDTO
{
    public decimal TotalSold { get; set; }
    public decimal TotalBought { get; set; }
    public decimal Difference => TotalSold - TotalBought;
}
