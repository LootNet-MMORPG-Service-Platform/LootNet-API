using LootNet_API.DTO;

namespace LootNet_API.DTO.Admin;

public class UpdateMarketplaceEconomyDTO
{
    public decimal DailyCurrencyReward { get; set; }
    public decimal BotBasePrice { get; set; }
    public decimal BotStatMultiplier { get; set; }
    public decimal BotElementMultiplier { get; set; }
    public bool IsPlayerToPlayerTaxEnabled { get; set; }
    public bool IsPlayerToBotTaxEnabled { get; set; }
    public List<MarketTaxBracketDTO> ProgressiveTaxBrackets { get; set; } = new();
}
