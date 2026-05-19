using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateRuleFullDTO
{
    [NotEmptyGuid]
    public Guid ProfileId { get; set; }

    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public bool IsFallback { get; set; }

    [MaxCollectionCount(20)]
    public List<CreateParameterDTO> Parameters { get; set; } = new();

    [MaxCollectionCount(20)]
    public List<CreateElementDTO> Elements { get; set; } = new();
}
