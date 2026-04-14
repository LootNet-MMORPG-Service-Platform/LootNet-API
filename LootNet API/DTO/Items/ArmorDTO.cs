using LootNet_API.Enums;

namespace LootNet_API.DTO.Items;

public class ArmorDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ItemCategory Category { get; set; }
    public ArmorType ArmorType { get; set; }
    public double CutResistance { get; set; }
    public double BluntResistance { get; set; }
    public List<ItemElementDTO> Elements { get; set; } = [];
}
