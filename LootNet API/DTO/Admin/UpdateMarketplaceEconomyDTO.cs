using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.Admin;

public class UpdateMarketplaceEconomyDTO
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal DailyCurrencyReward { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal BotBasePrice { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal BotStatMultiplier { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal BotElementMultiplier { get; set; }

    public bool IsPlayerToPlayerTaxEnabled { get; set; }
    public bool IsPlayerToBotTaxEnabled { get; set; }

    [Required]
    [MinCollectionCount(1)]
    [MaxCollectionCount(20)]
    public List<MarketTaxBracketDTO> ProgressiveTaxBrackets { get; set; } = new();
}
