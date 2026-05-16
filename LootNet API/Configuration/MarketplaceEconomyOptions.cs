namespace LootNet_API.Configuration;

public class MarketplaceEconomyOptions
{
    public decimal DailyCurrencyReward { get; set; } = 75m;
    public decimal BotBasePrice { get; set; } = 20m;
    public decimal BotStatMultiplier { get; set; } = 8m;
    public decimal BotElementMultiplier { get; set; } = 1.5m;
    public bool IsPlayerToPlayerTaxEnabled { get; set; } = true;
    public bool IsPlayerToBotTaxEnabled { get; set; }
    public List<MarketplaceTaxBracketOptions> ProgressiveTaxBrackets { get; set; } = new()
    {
        new MarketplaceTaxBracketOptions { From = 0m, To = 500m, Rate = 0.05m },
        new MarketplaceTaxBracketOptions { From = 500m, To = 2_000m, Rate = 0.10m },
        new MarketplaceTaxBracketOptions { From = 2_000m, To = 10_000m, Rate = 0.18m },
        new MarketplaceTaxBracketOptions { From = 10_000m, To = null, Rate = 0.25m }
    };
}

public class MarketplaceTaxBracketOptions
{
    public decimal From { get; set; }
    public decimal? To { get; set; }
    public decimal Rate { get; set; }
}
