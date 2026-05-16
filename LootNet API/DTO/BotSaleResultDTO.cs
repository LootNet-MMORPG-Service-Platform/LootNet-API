using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class BotSaleResultDTO
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public ItemCategory Category { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CurrencyAfterSale { get; set; }
}
