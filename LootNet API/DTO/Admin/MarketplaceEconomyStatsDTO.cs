namespace LootNet_API.DTO.Admin;

public class MarketplaceEconomyStatsDTO
{
    public decimal TotalCurrencyHeldByPlayers { get; set; }
    public decimal TotalP2PVolume { get; set; }
    public decimal TotalBotSaleVolume { get; set; }
    public decimal TotalTaxRemoved { get; set; }
    public decimal TotalP2PTaxRemoved { get; set; }
    public decimal TotalBotTaxRemoved { get; set; }
    public int ActiveListings { get; set; }
    public decimal ActiveListingsValue { get; set; }
    public int TransactionCount { get; set; }
}
