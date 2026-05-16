using LootNet_API.DTO.Items;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Market;

public class SellInventoryItemDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemCategory Category { get; set; }
    public string ItemKind { get; set; } = string.Empty; // weapon|armor
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public double? Cut { get; set; }
    public double? Blunt { get; set; }
    public double? CutResistance { get; set; }
    public double? BluntResistance { get; set; }
    public List<ItemElementDTO> Elements { get; set; } = [];
    public double PowerScore { get; set; }
}
