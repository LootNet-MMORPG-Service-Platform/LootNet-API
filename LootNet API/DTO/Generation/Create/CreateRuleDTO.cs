using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateRuleDTO
{
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }
}
