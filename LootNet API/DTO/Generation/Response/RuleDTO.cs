using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Response;

public class RuleDTO
{
    public Guid Id { get; set; }
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }

    public List<ParameterDTO> Parameters { get; set; } = new();
    public List<ElementDTO> Elements { get; set; } = new();
}