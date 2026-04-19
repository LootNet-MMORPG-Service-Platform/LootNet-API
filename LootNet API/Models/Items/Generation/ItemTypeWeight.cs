using LootNet_API.Enums;

namespace LootNet_API.Models.Items.Generation;

public class ItemTypeWeight
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public ItemCategory Category { get; set; }

    public WeaponType? WeaponType { get; set; }

    public ArmorType? ArmorType { get; set; }

    public double Weight { get; set; }
}
