using LootNet_API.Enums;

namespace LootNet_API.DTO.Items;

public class ItemRewardDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ItemCategory Category { get; set; }
    public decimal CurrencyReward { get; set; }
    public decimal CurrencyAfterReward { get; set; }
}
