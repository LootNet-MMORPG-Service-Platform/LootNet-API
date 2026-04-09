using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class ItemGenerationRule
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public ItemCategory Category { get; set; }

    public WeaponType? WeaponType { get; set; }

    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }

    public List<ItemParameterSetting> Parameters { get; set; } = new();
    public List<ItemElementSetting> Elements { get; set; } = new();
}
