using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Update;

public class UpdateRuleDTO
{
    public Guid Id { get; set; }
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }
}
