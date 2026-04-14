using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateRuleFullDTO
{
    public Guid ProfileId { get; set; }
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }

    public List<CreateParameterDTO> Parameters { get; set; } = new();
    public List<CreateElementDTO> Elements { get; set; } = new();
}
