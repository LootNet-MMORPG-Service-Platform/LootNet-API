using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class MarketListingDTO
{
    public Guid Id { get; set; }
    public string ItemName { get; set; }
    public decimal Price { get; set; }
    public Guid SellerId { get; set; }
    public ItemCategory Category { get; set; }
}
