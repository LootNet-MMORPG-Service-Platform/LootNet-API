using LootNet_API.Enums;

namespace LootNet_API.Services.Interfaces;

public interface IItemNameGenerator
{
    string GenerateWeaponName(WeaponType type);
    string GenerateArmorName(ArmorType type);
}
