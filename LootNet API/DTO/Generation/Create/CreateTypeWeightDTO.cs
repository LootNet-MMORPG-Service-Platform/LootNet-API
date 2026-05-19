using System.ComponentModel.DataAnnotations;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateTypeWeightDTO
{
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }
}

