namespace LootNet_API.DTO.Generation.Create;

using Enums;

public class CreateTypeWeightDTO
{
    public ItemCategory Category { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ArmorType? ArmorType { get; set; }
    public double Weight { get; set; }
}
