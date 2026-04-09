namespace LootNet_API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public Guid SellerId { get; set; }
        public Guid ItemId { get; set; }
        public int Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
