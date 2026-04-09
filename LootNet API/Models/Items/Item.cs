using LootNet_API.Enums;

namespace LootNet_API.Models.Items;
public abstract class Item
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ItemCategory Category { get; set; }
    public Guid OwnerId { get; set; }
}

