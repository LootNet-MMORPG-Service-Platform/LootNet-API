using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Response;

public class TypeWeightDTO
{
    public Guid Id { get; set; }
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public double Weight { get; set; }
}
