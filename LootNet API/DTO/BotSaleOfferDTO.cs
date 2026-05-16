using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class BotSaleOfferDTO
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public ItemCategory Category { get; set; }
    public decimal StatScore { get; set; }
    public decimal OfferedPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SellerPayout { get; set; }
}
