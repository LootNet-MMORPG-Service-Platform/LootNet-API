namespace LootNet_API.Models.Market;
using Enums;
public class MarketListing
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid ItemId { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSold { get; set; } = false;

    public ItemCategory Category { get; set; }
}

