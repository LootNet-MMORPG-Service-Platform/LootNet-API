namespace LootNet_API.DTO;

public class MarketSaleTaxDTO
{
    public decimal GrossPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SellerPayout { get; set; }
    public decimal EffectiveTaxRate { get; set; }
}
