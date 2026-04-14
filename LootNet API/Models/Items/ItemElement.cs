using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class ItemElement
{
    public Guid Id { get; set; }
    public Guid? ArmorId { get; set; }
    public Guid? WeaponId { get; set; }
    public ItemElementType ItemElementType { get; set; }
    public double Value { get; set; }
}
